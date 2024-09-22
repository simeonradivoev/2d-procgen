using ProcGen2D;
using Unity.Mathematics;

namespace DefaultNamespace
{
    public interface IBiomeSource
    {
        GeneratorBiome GetBiome(int2 chunk, int seed, out int depth);
    }
}