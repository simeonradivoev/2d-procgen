using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D
{
    public class GeneratorContext
    {
        public GameObject Generator;

        public uint WorldSeed;
    }

    public interface IGenerationContext
    {
        public int2 Chunk { get; }

        public int2 ChunkWorldMin { get; }

        public SharedData SharedData { get; }
    }

    public struct LayerContext : IGenerationContext
    {
        public uint Seed { get; }

        public LayerContext(uint seed, int2 chunk, int2 chunkWorldMin, SharedData saveData)
        {
            Seed = seed;
            Chunk = chunk;
            ChunkWorldMin = chunkWorldMin;
            SharedData = saveData;
        }

        #region Implementation of IGenerationContext

        public int2 Chunk { get; }

        public int2 ChunkWorldMin { get; }

        public SharedData SharedData { get; }

        #endregion
    }
}