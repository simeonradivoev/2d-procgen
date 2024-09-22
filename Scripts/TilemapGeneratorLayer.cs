using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = Unity.Mathematics.Random;

namespace ProcGen2D
{
    public class TilemapGeneratorLayer : GeneratorLayer
    {
        [SerializeField]
        private TilemapEntry[] _tiles;

        [SerializeField]
        private bool _randomTilePerChunk;

        [SerializeField]
        [Range(0, 1)]
        [Tooltip("Only values above this will be placed")]
        private float _threshold = 0.5f;

        [SerializeField]
        [Tooltip("Use the values as a random weight if a tile should be placed")]
        private bool _useAsWeight;

        private NativeArray<byte> _changesNative;

        private NativeArray<float> _tilesNative;

        private float _totalTileWeight;

        #region Overrides of GeneratorLayer

        public override void Dispose()
        {
            base.Dispose();
            _tilesNative.Dispose();
            _changesNative.Dispose();
        }

        #endregion

        protected override void InitializeInternal(int2 size, GeneratorContext context)
        {
            _tilesNative = new NativeArray<float>(_tiles.Length, Allocator.Persistent);
            _changesNative = new NativeArray<byte>(size.x * size.y, Allocator.Persistent);

            for (var i = 0; i < _tiles.Length; i++)
            {
                _totalTileWeight += _tiles[i].Weight;
                _tilesNative[i] = _tiles[i].Weight;
            }
        }

        private void ClearChangesNative()
        {
            unsafe
            {
                long size = UnsafeUtility.SizeOf<byte>() * _changesNative.Length;
                UnsafeUtility.MemClear(_changesNative.GetUnsafePtr(), size);
            }
        }

        protected override void PostGenerateInternal(IGenerationContext genContext)
        {
            if (_tiles.Length > 0)
            {
                if (genContext is TilemapContext tilemapContext)
                {
                    SyncChanges(tilemapContext.ChangeData);
                }
            }
        }

        [Serializable]
        private class TilemapEntry
        {
            public float Weight = 1f;

            public TileBase Tile;
        }

        private struct TilemapEntryNative
        {
            private Color m_Color;

            private Vector3Int m_Position;

            private IntPtr m_TileAsset;

            private Matrix4x4 m_Transform;
        }

        private struct SyncChangesJob : IJob
        {
            public NativeArray<TilemapEntryNative> Entries;

            public NativeArray<TileChangeData> TilemapChanges;

            public NativeArray<byte> ChangeData;

            public int2 Size;

            #region Implementation of IJob

            public void Execute()
            {
            }

            #endregion
        }

        [BurstCompile]
        private struct TilemapUpdateJob : IJob
        {
            public NativeArray<float> Entries;

            public NativeArray<byte> ChangeData;

            public NativeArray<float> Data;

            public float TotalTileWeight;

            public uint Seed;

            public float Threshold;

            public int2 Size;

            public int2 TilemapChunkMin;

            public bool RandomTilePerChunk;

            public bool UseAsWeight;

            #region Implementation of IJob

            public void Execute()
            {
                var random = new Random(Seed);
                var tile = GetRandomTile(ref random);
                var thresholdInv = 1 - Threshold;

                for (var y = 0; y < Size.y; y++)
                {
                    for (var x = 0; x < Size.x; x++)
                    {
                        var flatIndex = y * Size.x + x;
                        var pos = new Vector3Int(TilemapChunkMin.x + x, TilemapChunkMin.y + y, 0);

                        if (!RandomTilePerChunk)
                        {
                            tile = GetRandomTile(ref random);
                        }

                        if (Data[flatIndex] >= Threshold)
                        {
                            if (UseAsWeight)
                            {
                                if (random.NextFloat() < math.max(Data[flatIndex] - Threshold, 0) / thresholdInv)
                                {
                                    ChangeData[flatIndex] = (byte)(tile + 1);
                                }
                            }
                            else
                            {
                                ChangeData[flatIndex] = (byte)(tile + 1);
                            }
                        }
                    }
                }
            }

            #endregion

            private int GetRandomTile(ref Random random)
            {
                var randomWeight = random.NextFloat(0, TotalTileWeight);
                for (var i = 0; i < Entries.Length; i++)
                {
                    var tile = Entries[i];
                    randomWeight -= tile;
                    if (randomWeight < 0)
                    {
                        return i;
                    }
                }

                return 0;
            }
        }

        #region Overrides of GeneratorLayer

        protected override JobHandle GenerateAsync(uint seed, IGenerationContext genContext, JobHandle genHandle, NativeArray<float> data)
        {
            ClearChangesNative();

            if (CreateJob(seed, genContext, data, out var job))
            {
                return job.Schedule(genHandle);
            }

            return genHandle;
        }

        private void SyncChanges(TileChangeData[] changeData)
        {
            for (var i = 0; i < _changesNative.Length; i++)
            {
                if (_changesNative[i] > 0)
                {
                    changeData[i] = new TileChangeData(
                        new Vector3Int(i % Size.x, i / Size.x),
                        _tiles[_changesNative[i] - 1].Tile,
                        Color.white,
                        Matrix4x4.identity);
                }
            }
        }

        private bool CreateJob(uint seed, IGenerationContext genContext, NativeArray<float> data, out TilemapUpdateJob job)
        {
            if (_tiles.Length > 0)
            {
                if (genContext is TilemapContext context)
                {
                    job = new TilemapUpdateJob
                    {
                        Size = Size,
                        TilemapChunkMin = context.TilemapChunkMin,
                        Data = data,
                        Seed = seed,
                        Entries = _tilesNative,
                        UseAsWeight = _useAsWeight,
                        Threshold = _threshold,
                        ChangeData = _changesNative,
                        RandomTilePerChunk = _randomTilePerChunk,
                        TotalTileWeight = _totalTileWeight
                    };
                    return true;
                }
            }

            job = default;
            return false;
        }

        protected override void Generate(uint seed, IGenerationContext genContext, NativeArray<float> data)
        {
            ClearChangesNative();

            if (CreateJob(seed, genContext, data, out var job))
            {
                job.Run();
            }
        }

        #endregion
    }
}