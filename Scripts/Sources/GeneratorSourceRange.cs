using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D.Layers
{
    [Serializable]
    [BurstCompile]
    public struct RangeJob : IJob
    {
        public float2 Range;

        public bool Mask;

        public NativeContext Context;

        #region Implementation of IJob

        public void Execute()
        {
            if (Mask)
            {
                for (var i = 0; i < Context.Data.Length; i++)
                {
                    Context.Data[i] = Context.Data[i] >= Range.x && Context.Data[i] <= Range.y ? 1 : 0;
                }
            }
            else
            {
                var distance = Range.y - Range.x;
                for (var i = 0; i < Context.Data.Length; i++)
                {
                    Context.Data[i] = math.saturate(math.max(0, Context.Data[i] - Range.x) / distance);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Clamp value within a range.
    /// </summary>
    public class GeneratorSourceRange : GeneratorSourceBase
    {
        [SerializeField]
        private RangeJob _settings;

        public override void GenerateAsync(LayerContext context, NativeArray<float> data, JobHandle other, out JobHandle handle)
        {
            _settings.Context = new NativeContext { Size = Size, Seed = context.Seed, Data = data };
            handle = _settings.ScheduleByRef(other);
        }
    }
}