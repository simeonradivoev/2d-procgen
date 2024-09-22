using System;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D.Layers
{
    [Serializable]
    [BurstCompile]
    public struct HorizontalGradientJob : IJob
    {
        public bool Sine;

        public float Multiplier;

        public NativeContext Context;

        #region Implementation of IJob

        public void Execute()
        {
            if (Sine)
            {
                for (var y = 0; y < Context.Size.y; y++)
                {
                    for (var x = 0; x < Context.Size.x; x++)
                    {
                        Context.Data[y * Context.Size.x + x] = math.sin(x / (float)Context.Size.x * math.PI) * Multiplier;
                    }
                }
            }
            else
            {
                for (var y = 0; y < Context.Size.y; y++)
                {
                    for (var x = 0; x < Context.Size.x; x++)
                    {
                        Context.Data[y * Context.Size.x + x] = x / (float)Context.Size.x * Multiplier;
                    }
                }
            }
        }

        #endregion
    }

    public class GeneratorSourceHorizontalGradient : JobGeneratorSource<HorizontalGradientJob>
    {
        [SerializeField]
        private HorizontalGradientJob _settings;

        #region Overrides of JobGeneratorSource<HorizontalGradientJob>

        protected override ref HorizontalGradientJob CreateJob(LayerContext layerContext, NativeContext context)
        {
            _settings.Context = context;
            return ref _settings;
        }

        protected override JobHandle ScheduleJob(ref HorizontalGradientJob job, JobHandle previous)
        {
            return job.ScheduleByRef();
        }

        #endregion
    }
}