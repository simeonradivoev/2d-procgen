using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace ProcGen2D
{
    public class SharedData
    {
        private readonly Dictionary<string, NativeArray<float>> _sharedData = new();

        private readonly Dictionary<string, JobHandle> _handles = new();

        public bool ContainsData(string id)
        {
            return _sharedData.ContainsKey(id);
        }

        public bool TryGetData(string id, out NativeArray<float> data)
        {
            return _sharedData.TryGetValue(id, out data);
        }

        public bool TryAdd(string id, NativeArray<float> data)
        {
            return _sharedData.TryAdd(id, data);
        }

        public void AddHandle(string id, JobHandle handle)
        {
            _handles.Add(id, handle);
        }

        public JobHandle GetHandle(string id)
        {
            _handles.TryGetValue(id, out var handle);
            return handle;
        }

        public void Clear()
        {
            ClearData();
            ClearHandles();
        }

        public void ClearData()
        {
            foreach (var kvp in _sharedData)
            {
                kvp.Value.Dispose();
            }

            _sharedData.Clear();
        }

        public void ClearHandles()
        {
            _handles.Clear();
        }
    }
}