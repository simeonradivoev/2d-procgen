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
    public struct SharedDataJob : IJob
    {
        public bool Invert;

        public float2 MaskRange;

        public bool Mask;

        [ReadOnly]
        public NativeArray<float> SharedData;

        public NativeContext Context;

        #region Implementation of IJob

        public void Execute()
        {
            var distance = MaskRange.y - MaskRange.x;
            for (var i = 0; i < SharedData.Length; i++)
            {
                float data;
                if (Invert)
                {
                    data = 1 - math.saturate(SharedData[i]);
                }
                else
                {
                    data = SharedData[i];
                }

                if (Mask)
                {
                    data = data >= MaskRange.x && data <= MaskRange.y ? 1 : 0;
                }
                else
                {
                    data = math.saturate(math.max(0, data - MaskRange.x) / distance);
                }

                Context.Data[i] = data;
            }
        }

        #endregion
    }

    public class GeneratorSourceSharedData : JobGeneratorSource<SharedDataJob>
    {
        [SerializeField]
        private string _dataId;

        [SerializeField]
        private SharedDataJob _settings = new() { MaskRange = new float2(0, 1) };

        protected override JobHandle GenerateAsyncInternal(LayerContext context, NativeArray<float> data, JobHandle previous)
        {
            _settings.Context = new NativeContext
            {
                Data = data, ChunkWorldMin = context.ChunkWorldMin, Chunk = context.Chunk, Size = Size, Seed = context.Seed
            };
            if (context.SharedData.TryGetData(_dataId, out var sharedData))
            {
                _settings.SharedData = sharedData;
            }
            else
            {
                Debug.LogError($"Could not find shaded data with ID {_dataId}");
                _settings.SharedData = default;
            }
            var handle = _settings.ScheduleByRef(context.SharedData.GetHandle(_dataId));
            return handle;
        }

        #region Overrides of JobGeneratorSource<SharedDataJob>

        protected override ref SharedDataJob CreateJob(LayerContext layerContext, NativeContext context)
        {
            return ref _settings;
        }

        protected override JobHandle ScheduleJob(ref SharedDataJob job, JobHandle previous)
        {
            return job.ScheduleByRef();
        }

        #endregion
    }
}