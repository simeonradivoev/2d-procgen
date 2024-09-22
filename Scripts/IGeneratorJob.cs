using Unity.Mathematics;

namespace ProcGen2D
{
    public interface IGeneratorJob
    {
        public int Seed { get; set; }

        public int2 Size { get; set; }
    }
}