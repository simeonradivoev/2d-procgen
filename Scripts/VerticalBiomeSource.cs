using ProcGen2D;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DefaultNamespace
{
    /// <summary>
    /// Provides biomes vertically infinitely based on random weight.
    /// </summary>
    public class VerticalBiomeSource : MonoBehaviour, IBiomeSource
    {
        [SerializeField]
        private List<GeneratorBiome> _biomes;

        private float _totalBiomeWeight;

        public IReadOnlyList<GeneratorBiome> Biomes => _biomes;

        #region Implementation of IBiomeSource

        public GeneratorBiome GetBiome(int2 chunk, int seed, out int depth)
        {
            var random = new Random(math.hash(new int3(chunk.x, chunk.y, seed)));
            var biomeIndex = _biomes.WeightedRandom(_totalBiomeWeight, b => b.Weight, ref random);

            if (biomeIndex < 0)
            {
                throw new Exception("Invalid Biome Weights.");
            }

            depth = biomeIndex;
            return _biomes[biomeIndex];
        }

        #endregion

        public void Initialize()
        {
            if (_biomes.Count <= 0)
            {
                GetComponentsInChildren(_biomes);
            }
            _totalBiomeWeight = _biomes.Sum(b => b.Weight);
            foreach (var biome in _biomes)
            {
                biome.Initialize();
            }
        }
    }
}