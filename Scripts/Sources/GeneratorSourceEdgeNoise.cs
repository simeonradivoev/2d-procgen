using System;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D.Layers
{
    [Serializable]
    [BurstCompile]
    public struct EdgeNoiseJob : IJob
    {
        public int EdgeWallPadding;

        public int SeedOffset;

        public float EdgeNoiseSpread;

        public float EdgeNoiseSize;

        public NativeContext Context;

        #region Implementation of IJob

        public void Execute()
        {
            for (var x = 0; x < Context.Size.x; x++)
            {
                for (var y = 0; y < Context.Size.y; y++)
                {
                    var flatIndex = y * Context.Size.x + x;

                    if (x < EdgeWallPadding || y >= Context.Size.y - EdgeWallPadding)
                    {
                        Context.Data[flatIndex] = 1;
                        return;
                    }

                    float noiseValue;
                    var halfWidth = Context.Size.x / 2;
                    if (x < halfWidth)
                    {
                        var percent = Mathf.Max(0, x - EdgeWallPadding) / (halfWidth - EdgeWallPadding);
                        noiseValue = noise.cnoise(new float2(Context.ChunkWorldMin.y * EdgeNoiseSize, Context.Seed + SeedOffset)) * EdgeNoiseSpread +
                                     percent;
                    }
                    else
                    {
                        var percent = Mathf.Max(0, Context.Size.x - x - EdgeWallPadding) / (halfWidth - EdgeWallPadding);
                        noiseValue = noise.cnoise(new float2(Context.ChunkWorldMin.y * EdgeNoiseSize, Context.Seed + 1 + SeedOffset)) *
                                     EdgeNoiseSpread +
                                     percent;
                    }

                    Context.Data[flatIndex] = noiseValue;
                }
            }
        }

        #endregion
    }

    public class GeneratorSourceEdgeNoise : JobGeneratorSource<EdgeNoiseJob>
    {
        [SerializeField]
        private EdgeNoiseJob _settings = new() { EdgeWallPadding = 1, EdgeNoiseSpread = 0.2f, EdgeNoiseSize = 0.1f };

        #region Overrides of JobGeneratorSource<EdgeNoiseJob>

        protected override JobHandle ScheduleJob(ref EdgeNoiseJob job, JobHandle previous)
        {
            return job.ScheduleByRef();
        }

        protected override ref EdgeNoiseJob CreateJob(LayerContext layerContext, NativeContext context)
        {
            _settings.Context = context;
            return ref _settings;
        }

        #endregion
    }
}