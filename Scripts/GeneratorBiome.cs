using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGen2D
{
    public class GeneratorBiome : MonoBehaviour, IDisposable
    {
        [SerializeField]
        private float _weight = 1;

        private readonly Dictionary<string, GeneratorGroup> _biomesMap = new();

        public float Weight => _weight;

        #region IDisposable

        public void Dispose()
        {
            foreach (var kvp in _biomesMap)
            {
                kvp.Value.Dispose();
            }
        }

        #endregion

        public void Initialize()
        {
            foreach (var generatorBiome in GetComponentsInChildren<GeneratorGroup>())
            {
                if (!_biomesMap.TryAdd(generatorBiome.name, generatorBiome))
                {
                    Debug.LogError("Foound multiple biomes with ");
                }
            }
        }

        public bool TryGetGroup(string id, out GeneratorGroup group)
        {
            return _biomesMap.TryGetValue(id, out group);
        }
    }
}