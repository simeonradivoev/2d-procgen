using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D
{
    public class GeneratorLayer : MonoBehaviour, IDisposable
    {
        [SerializeField]
        [Tooltip("ID for the data to be saved so that it can be accessed by other layers")]
        private string _dataId;

        private readonly List<GeneratorSourceBase> _sources = new();

        private bool _initialized;

        public int2 Size { get; private set; }

        #region IDisposable

        public virtual void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            foreach (var layer in _sources)
            {
                if (layer is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        #endregion

        protected virtual void Generate(uint seed, IGenerationContext context, NativeArray<float> data)
        {
        }

        protected virtual JobHandle GenerateAsync(uint seed, IGenerationContext context, JobHandle generationHandle, NativeArray<float> data)
        {
            return generationHandle;
        }

        protected virtual void PostGenerateInternal(IGenerationContext genContext)
        {
        }

        public void PostGenerate(IGenerationContext genContext)
        {
            PostGenerateInternal(genContext);
        }

        private void GetData(IGenerationContext context, out NativeArray<float> data, out bool sharedData)
        {
            sharedData = !string.IsNullOrEmpty(_dataId);
            data = new NativeArray<float>(Size.x * Size.y, Allocator.Persistent);

            if (sharedData)
            {
                if (!context.SharedData.TryAdd(_dataId, data))
                {
                    throw new Exception($"Data with the ID {_dataId} already saved elsewhere");
                }
            }
        }

        public JobHandle GenerateAsync(uint seed, IGenerationContext context)
        {
            GetData(context, out var data, out var sharedData);

            JobHandle previous = default;

            for (var i = 0; i < _sources.Count; i++)
            {
                var layerContext = new LayerContext(math.hash(new uint2(seed, (uint)i)), context.Chunk, context.ChunkWorldMin, context.SharedData);
                _sources[i].GenerateAsync(layerContext, data, previous, out var handle);

                //_sources[i].Generate(layerContext, data);

                previous = handle;
            }

            var finalHandle = GenerateAsync(seed, context, previous, data);
            if (sharedData)
            {
                context.SharedData.AddHandle(_dataId, finalHandle);
            }
            return finalHandle;
        }

        protected virtual void InitializeInternal(int2 size, GeneratorContext context)
        {
        }

        public void Initialize(int2 size, GeneratorContext context)
        {
            _initialized = true;

            GetComponentsInChildren(_sources);
            _sources.RemoveAll(s => !s.enabled);

            Size = size;

            InitializeInternal(size, context);

            foreach (var layer in _sources)
            {
                layer.Initialize(size, context);
            }
        }
    }
}