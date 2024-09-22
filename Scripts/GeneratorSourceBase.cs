using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D
{
    public abstract class GeneratorSourceBase : MonoBehaviour
    {
        public int2 Size { get; private set; }

        protected virtual void Start()
        {
        }

        protected virtual void InitializeInternal(int2 size, GeneratorContext context)
        {
        }

        public void Initialize(int2 size, GeneratorContext context)
        {
            Size = size;
            InitializeInternal(size, context);
        }

        public abstract void GenerateAsync(LayerContext context, NativeArray<float> data, JobHandle previous, out JobHandle handle);
    }
}