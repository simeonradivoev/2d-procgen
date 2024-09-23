using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ProcGen2D
{
    public abstract class GeneratorSource : GeneratorSourceBase
    {
        [SerializeField]
        private GeneratorLayerOperation _operation;

        protected abstract JobHandle GenerateAsyncInternal(LayerContext context, NativeArray<float> data, JobHandle previous);

        public override void GenerateAsync(LayerContext context, NativeArray<float> data, JobHandle previous, out JobHandle handle)
        {
            var dataTmp = new NativeArray<float>(data.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var genHandle = GenerateAsyncInternal(context, dataTmp, previous);

            var combinedHandle = JobHandle.CombineDependencies(genHandle, previous);

            switch (_operation)
            {
                case GeneratorLayerOperation.Add:
                    handle = new AddJob(data, dataTmp).Schedule(combinedHandle);
                    break;

                case GeneratorLayerOperation.Subtract:
                    handle = new SubJob(data, dataTmp).Schedule(combinedHandle);
                    break;

                case GeneratorLayerOperation.Multiply:
                    handle = new MulJob(data, dataTmp).Schedule(combinedHandle);
                    break;

                case GeneratorLayerOperation.Divide:
                    handle = new DivJob(data, dataTmp).Schedule(combinedHandle);
                    break;

                case GeneratorLayerOperation.Min:
                    handle = new MinJob(data, dataTmp).Schedule(combinedHandle);
                    break;

                case GeneratorLayerOperation.Max:
                    handle = new MaxJob(data, dataTmp).Schedule(combinedHandle);
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}