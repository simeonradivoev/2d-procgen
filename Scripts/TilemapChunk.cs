using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D
{
    public enum ChunkGenerationState { NonGenerated, Processing, Generated, Disposing, Disposed }

    public class TilemapChunk : MonoBehaviour
    {
        private readonly Dictionary<string, TilemapChunkTilemap> _tilemapMap = new();

        public Transform Objects { get; private set; }

        public int2 Chunk { get; set; }

        public BitField32 Edges { get; set; }

        public ChunkGenerationState State { get; set; }

        public IReadOnlyDictionary<string, TilemapChunkTilemap> Tilemaps => _tilemapMap;

        private void Awake()
        {
            foreach (var tilemap in GetComponentsInChildren<TilemapChunkTilemap>())
            {
                _tilemapMap.TryAdd(tilemap.name, tilemap);
            }

            Objects = new GameObject("Objects").transform;
            Objects.SetParent(transform, false);
            Objects.transform.localPosition = Vector3.zero;
            Objects.transform.localRotation = Quaternion.identity;
            Objects.transform.localScale = Vector3.one;
        }

        private void OnEnable()
        {
            Edges = default;
            Chunk = default;
            State = ChunkGenerationState.NonGenerated;
        }

        public void Initialize(int2 chunk, int2 size)
        {
            Chunk = chunk;

            foreach (var tilemap in _tilemapMap)
            {
                tilemap.Value.Size = size;
            }
        }
    }
}