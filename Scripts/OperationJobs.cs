using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ProcGen2D
{
    [BurstCompile]
    public struct AddJob : IJob
    {
        private NativeArray<float> _lhs;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        private NativeArray<float> _rhs;

        public AddJob(NativeArray<float> lhs, NativeArray<float> rhs)
        {
            _lhs = lhs;
            _rhs = rhs;
        }

        #region Implementation of IJob

        public void Execute()
        {
            for (var i = 0; i < _lhs.Length; i++)
            {
                _lhs[i] += _rhs[i];
            }
        }

        #endregion
    }

    [BurstCompile]
    public struct SubJob : IJob
    {
        private NativeArray<float> _lhs;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        private NativeArray<float> _rhs;

        public SubJob(NativeArray<float> lhs, NativeArray<float> rhs)
        {
            _lhs = lhs;
            _rhs = rhs;
        }

        #region Implementation of IJob

        public void Execute()
        {
            for (var i = 0; i < _lhs.Length; i++)
            {
                _lhs[i] -= _rhs[i];
            }
        }

        #endregion
    }

    [BurstCompile]
    public struct MulJob : IJob
    {
        private NativeArray<float> _lhs;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        private NativeArray<float> _rhs;

        public MulJob(NativeArray<float> lhs, NativeArray<float> rhs)
        {
            _lhs = lhs;
            _rhs = rhs;
        }

        #region Implementation of IJob

        public void Execute()
        {
            for (var i = 0; i < _lhs.Length; i++)
            {
                _lhs[i] *= _rhs[i];
            }
        }

        #endregion
    }

    [BurstCompile]
    public struct DivJob : IJob
    {
        private NativeArray<float> _lhs;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        private NativeArray<float> _rhs;

        public DivJob(NativeArray<float> lhs, NativeArray<float> rhs)
        {
            _lhs = lhs;
            _rhs = rhs;
        }

        #region Implementation of IJob

        public void Execute()
        {
            for (var i = 0; i < _lhs.Length; i++)
            {
                _lhs[i] /= _rhs[i];
            }
        }

        #endregion
    }

    [BurstCompile]
    public struct MinJob : IJob
    {
        private NativeArray<float> _lhs;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        private NativeArray<float> _rhs;

        public MinJob(NativeArray<float> lhs, NativeArray<float> rhs)
        {
            _lhs = lhs;
            _rhs = rhs;
        }

        #region Implementation of IJob

        public void Execute()
        {
            for (var i = 0; i < _lhs.Length; i++)
            {
                _lhs[i] = math.min(_lhs[i], _rhs[i]);
            }
        }

        #endregion
    }

    [BurstCompile]
    public struct MaxJob : IJob
    {
        private NativeArray<float> _lhs;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        private NativeArray<float> _rhs;

        public MaxJob(NativeArray<float> lhs, NativeArray<float> rhs)
        {
            _lhs = lhs;
            _rhs = rhs;
        }

        #region Implementation of IJob

        public void Execute()
        {
            for (var i = 0; i < _lhs.Length; i++)
            {
                _lhs[i] = math.max(_lhs[i], _rhs[i]);
            }
        }

        #endregion
    }
}