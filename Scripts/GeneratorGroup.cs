using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ProcGen2D
{
    public class GeneratorGroup : MonoBehaviour, IDisposable
    {
        private readonly List<GeneratorLayer> _layers = new();

        private GeneratorContext _generatorContext;

        private int2 _size;

        public int LayerCount => _layers.Count;

        #region IDisposable

        public void Dispose()
        {
            if (_generatorContext != null)
            {
                return;
            }

            foreach (var layer in _layers)
            {
                layer.Dispose();
            }
        }

        #endregion

        public void LazyInitialize(int2 size, GeneratorContext context)
        {
            if (_generatorContext != null)
            {
                return;
            }

            GetComponentsInChildren(_layers);
            Initialize(size, context);
        }

        public void Initialize(int2 size, GeneratorContext context)
        {
            _generatorContext = context;
            _size = size;
            foreach (var group in _layers)
            {
                group.Initialize(_size, context);
            }
        }

        public uint GetSeed(int layer, int2 chunk)
        {
            return math.hash(new uint3(_generatorContext.WorldSeed, (uint)layer, math.hash(chunk)));
        }

        /*public IEnumerator GenerateAsync(int layer, IGenerationContext context, CancellationToken cancellationToken = default)
        {
            var handle = _layers[layer].GenerateAsync(GetSeed(layer, context.Chunk), context);
            yield return new WaitUntil(() => handle.IsCompleted || cancellationToken.IsCancellationRequested);
            handle.Complete();
            _layers[layer].PostGenerate(context);
        }

        public void Generate(int layer, IGenerationContext context)
        {
            var handle = _layers[layer].GenerateAsync(GetSeed(layer, context.Chunk), context);
            handle.Complete();
            _layers[layer].PostGenerate(context);
        }

        public void Generate(IGenerationContext context)
        {
            var handles = new NativeArray<JobHandle>(_layers.Count, Allocator.Temp);
            for (var i = 0; i < _layers.Count; i++)
            {
                var handle = _layers[i].GenerateAsync(GetSeed(i, context.Chunk), context);
                handles[i] = handle;
            }

            JobHandle.CompleteAll(handles);
            handles.Dispose();

            for (var i = 0; i < _layers.Count; i++)
            {
                _layers[i].PostGenerate(context);
            }
        }*/

        public IEnumerator GenerateAsync(IGenerationContext context)
        {
            var handles = new NativeArray<JobHandle>(_layers.Count, Allocator.TempJob);
            for (var i = 0; i < _layers.Count; i++)
            {
                var handle = _layers[i].GenerateAsync(GetSeed(i, context.Chunk), context);
                handles[i] = handle;
            }

            JobHandle.ScheduleBatchedJobs();
            JobHandle.CompleteAll(handles);
            handles.Dispose();

            for (var i = 0; i < _layers.Count; i++)
            {
                _layers[i].PostGenerate(context);
                yield return null;
            }
        }
    }
}