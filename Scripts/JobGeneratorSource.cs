using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ProcGen2D.Layers
{
    public struct NativeContext
    {
        public NativeArray<float> Data;

        public int2 Chunk;

        public int2 ChunkWorldMin;

        public int2 Size;

        public uint Seed;
    }

    public abstract class JobGeneratorSource<T> : GeneratorSource where T : struct, IJob
    {
        protected abstract ref T CreateJob(LayerContext layerContext, NativeContext context);

        protected abstract JobHandle ScheduleJob(ref T job, JobHandle previous);

        #region Overrides of GeneratorLayer

        #region Overrides of GeneratorSource

        protected override JobHandle GenerateAsyncInternal(LayerContext context, NativeArray<float> data, JobHandle previous)
        {
            var job = CreateJob(
                context,
                new NativeContext { Data = data, ChunkWorldMin = context.ChunkWorldMin, Chunk = context.Chunk, Size = Size, Seed = context.Seed });
            return ScheduleJob(ref job, previous);
        }

        #endregion

        protected override void GenerateInternal(LayerContext context, NativeArray<float> data)
        {
            var job = CreateJob(
                context,
                new NativeContext { Data = data, ChunkWorldMin = context.ChunkWorldMin, Chunk = context.Chunk, Size = Size, Seed = context.Seed });
            job.RunByRef();
        }

        #endregion
    }
}