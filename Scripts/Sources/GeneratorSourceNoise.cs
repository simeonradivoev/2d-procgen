using System;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D.Layers
{
    public enum NoiseType { Perlin, CellularX, CellularY, Simplex }

    [Serializable]
    [BurstCompile]
    public struct PerlinJob : IJob, IGeneratorJob
    {
        public float NoiseSize;

        public float NoiseMultiplier;

        public bool Invert;

        public float NoiseOffset;

        public float Offset;

        public bool Clamp;

        public NoiseType NoiseType;

        [NonSerialized]
        public NativeContext Context;

        #region Implementation of IJob

        public void Execute()
        {
            switch (NoiseType)
            {
                case NoiseType.Perlin:
                    PerlinNoise();
                    break;

                case NoiseType.Simplex:
                    SimplexNoise();
                    break;

                case NoiseType.CellularX:
                    CellularNoiseX();
                    break;

                case NoiseType.CellularY:
                    CellularNoiseY();
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        private void PerlinNoise()
        {
            for (var y = 0; y < Size.y; y++)
            {
                for (var x = 0; x < Size.x; x++)
                {
                    var normalValue = (noise.cnoise(
                                           new float2(
                                               (Context.ChunkWorldMin.x + x + NoiseOffset) * NoiseSize,
                                               (Context.ChunkWorldMin.y + y + NoiseOffset) * NoiseSize)) +
                                       1) *
                                      0.5f;
                    var value = (normalValue + Offset) * NoiseMultiplier;
                    if (Invert)
                    {
                        value = 1 - value;
                    }
                    if (Clamp)
                    {
                        value = math.clamp(value, 0f, 1f);
                    }
                    Context.Data[y * Size.x + x] = value;
                }
            }
        }

        private void SimplexNoise()
        {
            for (var y = 0; y < Size.y; y++)
            {
                for (var x = 0; x < Size.x; x++)
                {
                    var normalValue = (noise.snoise(
                                           new float2(
                                               (Context.ChunkWorldMin.x + x + NoiseOffset) * NoiseSize,
                                               (Context.ChunkWorldMin.y + y + NoiseOffset) * NoiseSize)) +
                                       1) *
                                      0.5f;
                    var value = (normalValue + Offset) * NoiseMultiplier;
                    if (Invert)
                    {
                        value = 1 - value;
                    }
                    if (Clamp)
                    {
                        value = math.clamp(value, 0f, 1f);
                    }
                    Context.Data[y * Size.x + x] = value;
                }
            }
        }

        private void CellularNoiseX()
        {
            for (var y = 0; y < Size.y; y++)
            {
                for (var x = 0; x < Size.x; x++)
                {
                    var normalValue = (noise.cellular(
                                               new float2(
                                                   (Context.ChunkWorldMin.x + x + NoiseOffset) * NoiseSize,
                                                   (Context.ChunkWorldMin.y + y + NoiseOffset) * NoiseSize))
                                           .x +
                                       1) *
                                      0.5f;
                    var value = (normalValue + Offset) * NoiseMultiplier;
                    if (Invert)
                    {
                        value = 1 - value;
                    }
                    if (Clamp)
                    {
                        value = math.clamp(value, 0f, 1f);
                    }
                    Context.Data[y * Size.x + x] = value;
                }
            }
        }

        private void CellularNoiseY()
        {
            for (var y = 0; y < Size.y; y++)
            {
                for (var x = 0; x < Size.x; x++)
                {
                    var normalValue = (noise.cellular(
                                               new float2(
                                                   (Context.ChunkWorldMin.x + x + NoiseOffset) * NoiseSize,
                                                   (Context.ChunkWorldMin.y + y + NoiseOffset) * NoiseSize))
                                           .y +
                                       1) *
                                      0.5f;
                    var value = (normalValue + Offset) * NoiseMultiplier;
                    if (Invert)
                    {
                        value = 1 - value;
                    }
                    if (Clamp)
                    {
                        value = math.clamp(value, 0f, 1f);
                    }
                    Context.Data[y * Size.x + x] = value;
                }
            }
        }

        #region Implementation of IGeneratorJob

        public int Seed { get; set; }

        public int2 Size { get; set; }

        #endregion
    }

    public class GeneratorSourceNoise : JobGeneratorSource<PerlinJob>
    {
        [SerializeField]
        private PerlinJob _settings = new() { NoiseSize = 1, NoiseMultiplier = 1 };

        #region Overrides of

        protected override void InitializeInternal(int2 size, GeneratorContext context)
        {
            _settings.Size = size;
        }

        #endregion

        #region Overrides of JobGeneratorLayer<PerlinJob>

        protected override ref PerlinJob CreateJob(LayerContext layerContext, NativeContext context)
        {
            _settings.Context = context;
            return ref _settings;
        }

        protected override JobHandle ScheduleJob(ref PerlinJob job, JobHandle previous)
        {
            return job.ScheduleByRef();
        }

        #endregion
    }
}