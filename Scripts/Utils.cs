using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ProcGen2D
{
    public static class Utils
    {
        public static readonly int2[] OrthogonalDirections = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };

        public static readonly int2[] Directions = { new(0, 1), new(1, 1), new(1, 0), new(1, -1), new(0, -1), new(1, -1), new(-1, 0), new(-1, 1) };

        /// <summary>
        /// Direction index lookup fro each blending type in <see cref="BlendingDirection"/>. The indices refer to <see cref="Directions"/>.
        /// </summary>
        public static readonly int[][] BlendingDirections = { new[] { 2, 6 }, new[] { 0, 4 }, new[] { 0, 1, 2, 3, 4, 5, 6, 7 } };

        public static int WeightedRandom<T>(this IList<T> list, Func<T, float> weightGetter, ref Random random)
        {
            var totalWeight = 0f;
            for (var i = 0; i < list.Count; i++)
            {
                totalWeight += weightGetter.Invoke(list[i]);
            }

            return WeightedRandom(list, totalWeight, weightGetter, ref random);
        }

        public static int WeightedRandom<T>(this IList<T> list, float totalWeight, Func<T, float> weightGetter, ref Random random)
        {
            var randomWeight = random.NextFloat(0, totalWeight);

            for (var i = 0; i < list.Count; i++)
            {
                randomWeight -= weightGetter.Invoke(list[i]);
                if (randomWeight < 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Loops over an enumerator. Used to run coroutines fully and instantly.
        /// </summary>
        /// <param name="enumerator"></param>
        public static void ExecuteSync(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                switch (enumerator.Current)
                {
                    case WaitUntil until:
                        while (until.MoveNext())
                        {
                            Thread.Sleep(1);
                        }
                        break;

                    case WaitWhile waitWhile:
                        while (waitWhile.MoveNext())
                        {
                            Thread.Sleep(1);
                        }
                        break;

                    case YieldInstruction instruction:
                        break;

                    case IEnumerator other:
                        ExecuteSync(other);
                        break;
                }
            }
        }
    }
}