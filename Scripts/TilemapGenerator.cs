using DefaultNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    public enum BlendingDirection { Horizontal, Vertical, All }

    public class TilemapContext : IGenerationContext, IDisposable
    {
        public TileChangeData[] ChangeData { get; set; }

        public int2 TilemapChunkMin { get; set; }

        public BitField32 Edges { get; set; }

        public GeneratorBiome Biome { get; set; }

        #region Implementation of IDisposable

        public void Dispose()
        {
            SharedData.Clear();
        }

        #endregion

        #region Implementation of IGenerationContext

        public int2 Chunk { get; set; }

        public int2 ChunkWorldMin { get; set; }

        public SharedData SharedData { get; } = new();

        #endregion
    }

    public class TilemapGenerator : MonoBehaviour
    {
        [SerializeField]
        private uint _seed = 91597;

        [SerializeField]
        private int2 _chunkSize = new(32, 32);

        [SerializeField]
        private TilemapChunk _tilemapChunkPrefab;

        [SerializeField]
        private Vector2 _tileSize = Vector2.one;

        [SerializeField]
        private Transform _chunkParent;

        [SerializeField]
        private bool _chunkPooling = true;

        [SerializeField]
        private BlendingDirection _blendingDirections;

        [SerializeField]
        private int _blendingDistance = 16;

        private readonly Dictionary<string, Dictionary<int, TileChangeData[]>> _blendingChanges = new();

        private readonly Dictionary<int, float[]> _blendingMasks = new();

        private readonly Dictionary<string, TileChangeData[]> _changes = new();

        private readonly Stack<TilemapChunk> _chunkPool = new();

        private readonly Dictionary<int2, TilemapChunk> _chunks = new();

        private readonly TilemapContext _context = new();

        private readonly CancellationTokenSource _disposeToken = new();

        private readonly HashSet<int2> _disposingChunks = new();

        private readonly HashSet<int2> _generatingChunks = new();

        private readonly GeneratorContext _generatorContext = new();

        private TileBase[] _emptyTiles;

        private TileChangeData[] _rowChangeData;

        public uint Seed
        {
            get => _seed;
            set => _seed = value;
        }

        public Transform ChunkParent => _chunkParent;

        public int2 ChunkSize => _chunkSize;

        public TilemapChunk TilemapChunkPrefab => _tilemapChunkPrefab;

        public Vector2 TileSize => _tileSize;

        public IReadOnlyDictionary<int2, TilemapChunk> Chunks => _chunks;

        public IReadOnlyCollection<int2> GeneratingChunks => _generatingChunks;

        public IReadOnlyCollection<int2> DisposingChunks => _disposingChunks;

        public BlendingDirection BlendingDirections => _blendingDirections;

        private void OnDestroy()
        {
            _disposeToken.Cancel();
            _disposeToken.Dispose();
            _context.Dispose();
        }

        public void Initialize()
        {
            _emptyTiles = new TileBase[_chunkSize.x];
            _rowChangeData = new TileChangeData[_chunkSize.x];
            _generatorContext.WorldSeed = _seed;
            _generatorContext.Generator = gameObject;

            CalculateBlendingMasks();
        }

        public event Action<TilemapChunk> OnChunkGenerated;

        public event Action<TilemapChunk> OnChunkDispoed;

        private TilemapChunk GetChunk(int2 pos)
        {
            var localPos = new Vector3(pos.x * _chunkSize.x * _tileSize.x, pos.y * _chunkSize.y * _tileSize.y, 0);

            TilemapChunk chunk;
            if (_chunkPool.Count > 0 && _chunkPooling)
            {
                chunk = _chunkPool.Pop();
                chunk.gameObject.SetActive(true);
                chunk.State = ChunkGenerationState.NonGenerated;
            }
            else
            {
                chunk = Instantiate(_tilemapChunkPrefab, Vector3.zero, Quaternion.identity, _chunkParent);
            }

            chunk.transform.localPosition = localPos;
            return chunk;
        }

        public int2 WorldToChunkIndex(float2 pos)
        {
            return new int2((int)(pos.x / _tileSize.x) / _chunkSize.x, (int)(pos.y / _tileSize.y) / _chunkSize.y);
        }

        public float2 ChunkIndexToWorld(int2 chunk)
        {
            return new float2(chunk.x * _chunkSize.x * _tileSize.x, chunk.y * _chunkSize.x * _tileSize.y);
        }

        public int2 WorldToChunk(float2 pos)
        {
            return new int2((int)(pos.x / _tileSize.x), (int)(pos.y / _tileSize.y));
        }

        public float2 ChunkToWorld(int2 chunkLocal)
        {
            return new float2(chunkLocal.x * _tileSize.x, chunkLocal.y * _tileSize.y);
        }

        public bool TryGetWorldToChunkLocal(int2 chunk, float2 pos, out int2 chunkLocal)
        {
            var currentChunk = WorldToChunkIndex(pos);
            if (currentChunk.x != chunk.x || currentChunk.y != chunk.y)
            {
                chunkLocal = int2.zero;
                return false;
            }

            chunkLocal = WorldToChunk(pos);
            chunkLocal -= chunk * _chunkSize;
            return true;
        }

        private void CalculateBlendingMasks()
        {
            var blendDistanceInv = 1f / _blendingDistance;

            var directions = Utils.BlendingDirections[(int)_blendingDirections];
            foreach (var direction in directions)
            {
                var mask = new float[_chunkSize.x * _chunkSize.y];

                for (var y = 0; y < _chunkSize.y; y++)
                {
                    for (var x = 0; x < _chunkSize.x; x++)
                    {
                        var dir = Utils.Directions[direction];
                        var distance = 0f;
                        if (_blendingDirections is BlendingDirection.Horizontal or BlendingDirection.All)
                        {
                            if (dir.x > 0)
                            {
                                if (x > _chunkSize.x - _blendingDistance)
                                {
                                    distance = 1f - (_chunkSize.x - x) / (float)_blendingDistance - Mathf.PerlinNoise1D(y * blendDistanceInv) * 0.5f;
                                }
                            }
                            else
                            {
                                if (x < _blendingDistance)
                                {
                                    distance = 1f - x / (float)_blendingDistance - Mathf.PerlinNoise1D(y * blendDistanceInv) * 0.5f;
                                }
                            }
                        }

                        var horizontalDistance = distance;

                        if (_blendingDirections is BlendingDirection.Vertical or BlendingDirection.All)
                        {
                            if (dir.y > 0)
                            {
                                if (y > _chunkSize.y - _blendingDistance)
                                {
                                    distance = 1f - (_chunkSize.y - y) / (float)_blendingDistance - Mathf.PerlinNoise1D(x * blendDistanceInv) * 0.5f;
                                }
                            }
                            else
                            {
                                if (y < _blendingDistance)
                                {
                                    distance = 1f - y / (float)_blendingDistance - Mathf.PerlinNoise1D(x * blendDistanceInv) * 0.5f;
                                }
                            }
                        }

                        if (_blendingDirections is BlendingDirection.All)
                        {
                            distance = math.min(distance, horizontalDistance);
                        }

                        mask[y * _chunkSize.x + x] = distance;
                    }
                }

                _blendingMasks.Add(direction, mask);
            }
        }

        private void UpdateTilemaps(TilemapChunk chunk, int index)
        {
            var y = index / _chunkSize.x;

            foreach (var tilemap in chunk.Tilemaps)
            {
                if (_changes.TryGetValue(tilemap.Key, out var changes))
                {
                    Array.Copy(changes, index, _rowChangeData, 0, _chunkSize.x);

                    if (_blendingChanges.TryGetValue(tilemap.Key, out var blendingNeighborChanges))
                    {
                        foreach (var blendingChanges in blendingNeighborChanges)
                        {
                            if (chunk.Edges.IsSet(blendingChanges.Key))
                            {
                                var mask = _blendingMasks[blendingChanges.Key];
                                for (var mx = 0; mx < _chunkSize.x; mx++)
                                {
                                    var distance = mask[y * _chunkSize.x + mx];
                                    if (distance > 0.5f)
                                    {
                                        _rowChangeData[mx] = blendingChanges.Value[y * _chunkSize.x + mx];
                                    }
                                }
                            }
                        }
                    }

                    tilemap.Value.Tilemap.SetTiles(_rowChangeData, false);
                }
            }
        }

        private BitField32 CalculateEdges(IBiomeSource biomeSource, int2 chunk)
        {
            BitField32 edges = default;
            var biome = biomeSource.GetBiome(chunk, (int)_seed, out var depth);

            foreach (var i in Utils.BlendingDirections[(int)_blendingDirections])
            {
                var neighborDir = chunk + Utils.Directions[i];
                var neighborBiome = biomeSource.GetBiome(neighborDir, (int)_seed, out var neighborDepth);
                if (neighborBiome != biome && depth < neighborDepth)
                {
                    edges.SetBits(i, true);
                }
            }
            return edges;
        }

        private bool PrepareBlendingTilemap(IBiomeSource biomeSource, string id, int direction, int2 chunk, out GeneratorGroup group)
        {
            var biome = biomeSource.GetBiome(chunk, (int)_seed, out var depth);
            if (biome.TryGetGroup(id, out group))
            {
                if (!_blendingChanges.TryGetValue(id, out var changes))
                {
                    changes = new Dictionary<int, TileChangeData[]>();
                    _blendingChanges.Add(id, changes);
                }

                if (!changes.TryGetValue(direction, out var changeData))
                {
                    changeData = new TileChangeData[_chunkSize.x * _chunkSize.y];
                    changes.Add(direction, changeData);
                }
                else
                {
                    Array.Clear(changeData, 0, changeData.Length);
                }

                group.LazyInitialize(_chunkSize, _generatorContext);
                _context.Biome = biome;
                _context.Edges = default;
                _context.Chunk = chunk;
                _context.ChunkWorldMin = new int2(chunk.x * _chunkSize.x, chunk.y * +_chunkSize.y);
                _context.TilemapChunkMin = int2.zero;
                _context.ChangeData = changeData;
                return true;
            }

            return false;
        }

        private bool PrepareTilemap(IBiomeSource biomeSource, GeneratorBiome biome, string id, TilemapChunk chunk, out GeneratorGroup group)
        {
            if (biome.TryGetGroup(id, out group))
            {
                if (!_changes.TryGetValue(id, out var changeData))
                {
                    changeData = new TileChangeData[_chunkSize.x * _chunkSize.y];
                    _changes.Add(id, changeData);
                }
                else
                {
                    Array.Clear(changeData, 0, changeData.Length);
                }

                group.LazyInitialize(_chunkSize, _generatorContext);
                chunk.Edges = CalculateEdges(biomeSource, chunk.Chunk);
                _context.Edges = chunk.Edges;
                _context.Biome = biome;
                _context.Chunk = chunk.Chunk;
                _context.ChunkWorldMin = new int2(chunk.Chunk.x * _chunkSize.x, chunk.Chunk.y * +_chunkSize.y);
                _context.TilemapChunkMin = int2.zero;
                _context.ChangeData = changeData;
                return true;
            }

            return false;
        }

        private void FinishGen(TilemapChunk chunk)
        {
            RefreshNeighborTiles(chunk);
            _context.SharedData.Clear();
            chunk.State = ChunkGenerationState.Generated;
            _generatingChunks.Remove(chunk.Chunk);
            OnChunkGenerated?.Invoke(chunk);
        }

        private void InitChunk(TilemapChunk chunk, int2 chunkPos)
        {
            _generatingChunks.Add(chunkPos);
            chunk.name = $"Chunk {chunkPos.x},{chunkPos.y}";
            chunk.Chunk = chunkPos;
            chunk.Initialize(chunk.Chunk, _chunkSize);
            chunk.State = ChunkGenerationState.Processing;
            _chunks.Add(chunk.Chunk, chunk);
            UpdateNeighbors(chunk);
        }

        public void Generate(IBiomeSource biomeSource, int2 chunkPos)
        {
            Utils.ExecuteSync(GenerateAsync(biomeSource, chunkPos));
        }

        public IEnumerator GenerateAsync(IBiomeSource biomeSource, int2 chunkPos)
        {
            if (_chunks.ContainsKey(chunkPos))
            {
                throw new Exception($"Chunk {chunkPos} already exists");
            }

            var biome = biomeSource.GetBiome(chunkPos, (int)_seed, out var depth);

            var chunk = GetChunk(chunkPos);
            InitChunk(chunk, chunkPos);

            foreach (var tilemap in chunk.Tilemaps)
            {
                if (PrepareTilemap(biomeSource, biome, tilemap.Key, chunk, out var group))
                {
                    yield return group.GenerateAsync(_context);
                }
            }

            _context.SharedData.Clear();

            for (var i = 0; i < Utils.Directions.Length; i++)
            {
                if (chunk.Edges.IsSet(i))
                {
                    foreach (var tilemap in chunk.Tilemaps)
                    {
                        if (PrepareBlendingTilemap(biomeSource, tilemap.Key, i, chunkPos + Utils.Directions[i], out var neighborGroup))
                        {
                            yield return neighborGroup.GenerateAsync(_context);
                        }
                    }

                    _context.SharedData.Clear();
                }
            }

            for (var cy = 0; cy < _chunkSize.y; cy++)
            {
                UpdateTilemaps(chunk, cy * _chunkSize.x);
                yield return null;
            }

            FinishGen(chunk);
        }

        public IEnumerator DisposeAsync(int2 chunkPos)
        {
            if (_chunks.Remove(chunkPos, out var chunk))
            {
                chunk.State = ChunkGenerationState.Disposing;
                _disposingChunks.Add(chunkPos);

                for (var y = 0; y < _chunkSize.y; y++)
                {
                    // Gradual clearing of tiles prevents CPU spikes from physics updates
                    foreach (var tilemap in chunk.Tilemaps)
                    {
                        tilemap.Value.Tilemap.SetTilesBlock(new BoundsInt(0, y * _chunkSize.y, 0, _chunkSize.x, 1, 0), _emptyTiles);
                    }

                    yield return new WaitForEndOfFrame();
                }

                if (_chunkPooling)
                {
                    _chunkPool.Push(chunk);
                    chunk.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(chunk);
                }

                chunk.State = ChunkGenerationState.Disposed;
                _disposingChunks.Remove(chunkPos);
            }
        }

        private void RefreshNeighborTiles(TilemapChunk chunk)
        {
            for (var nx = 0; nx < 3; nx++)
            {
                for (var ny = 0; ny < 3; ny++)
                {
                    var neighborIndex = ny * 3 + nx;
                    var dir = new int2(nx - 1, ny - 1);
                    var neighborPos = chunk.Chunk + dir;
                    var neighborLocalPos = _chunkSize + dir % _chunkSize;

                    foreach (var tilemap in chunk.Tilemaps)
                    {
                        var neighbor = tilemap.Value.Neighbors[neighborIndex];

                        if (neighbor)
                        {
                            if (ny != 1)
                            {
                                for (var x = 0; x < _chunkSize.x; x++)
                                {
                                    neighbor.Tilemap.RefreshTile(new Vector3Int(neighborPos.x + x, neighborLocalPos.y));
                                }
                            }

                            if (nx != 1)
                            {
                                for (var y = 0; y < _chunkSize.y; y++)
                                {
                                    neighbor.Tilemap.RefreshTile(new Vector3Int(neighborLocalPos.x, neighborPos.y + y));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateNeighbors(TilemapChunk chunk)
        {
            for (var nx = 0; nx < 3; nx++)
            {
                for (var ny = 0; ny < 3; ny++)
                {
                    var neighborIndex = ny * 3 + nx;
                    if (neighborIndex == 4)
                    {
                        continue;
                    }

                    var neighborPos = chunk.Chunk + new int2(nx - 1, ny - 1);
                    var neighborIndexOpposite = (2 - ny) * 3 + (2 - nx);
                    if (_chunks.TryGetValue(neighborPos, out var neighbor))
                    {
                        foreach (var kvp in chunk.Tilemaps)
                        {
                            if (neighbor.Tilemaps.TryGetValue(kvp.Key, out var neighborTilemap))
                            {
                                kvp.Value.Neighbors[neighborIndex] = neighborTilemap;
                                neighborTilemap.Neighbors[neighborIndexOpposite] = kvp.Value;
                            }
                        }
                    }
                }
            }
        }
    }
}