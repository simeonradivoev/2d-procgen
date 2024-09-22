using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Action = System.Action;
using Random = Unity.Mathematics.Random;

namespace ProcGen2D
{
    public static class Utils
    {
        public static readonly int2[] OrthogonalDirections = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };

        public static readonly int2[] Directions = { new(0, 1), new(1, 1), new(1, 0), new(1, -1), new(0, -1), new(1, -1), new(-1, 0), new(-1, 1) };

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

        public struct JobHandleAwaiter : INotifyCompletion
        {
            private static readonly SendOrPostCallback RunActionCallback = RunAction;

            private static readonly WaitCallback WaitCallbackRunAction = RunAction;

            private readonly CancellationToken _cancellationToken;

            private readonly JobHandle _jobHandle;

            public bool IsCompleted => _jobHandle.IsCompleted || _cancellationToken.IsCancellationRequested;

            public JobHandleAwaiter(JobHandle jobHandle, CancellationToken cancellationToken = default)
            {
                _jobHandle = jobHandle;
                _cancellationToken = cancellationToken;
            }

            private static void RunAction(object action)
            {
                ((Action)action).Invoke();
            }

            public JobHandleAwaiter GetAwaiter()
            {
                return this;
            }

            public void GetResult()
            {
            }

            #region Implementation of INotifyCompletion

            public void OnCompleted(Action continuation)
            {
                _jobHandle.Complete();
                if (continuation == null)
                {
                    throw new ArgumentNullException(nameof(continuation));
                }
                var currentNoFlow = SynchronizationContext.Current;
                if (currentNoFlow != null && currentNoFlow.GetType() != typeof(SynchronizationContext))
                {
                    currentNoFlow.Post(RunActionCallback, continuation);
                }
                else
                {
                    var current = TaskScheduler.Current;
                    if (current == TaskScheduler.Default)
                    {
                        ThreadPool.QueueUserWorkItem(WaitCallbackRunAction, continuation);
                    }
                    else
                    {
                        Task.Factory.StartNew(continuation, new CancellationToken(), TaskCreationOptions.PreferFairness, current);
                    }
                }
            }

            #endregion
        }
    }
}