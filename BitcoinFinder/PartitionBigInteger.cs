using System.Numerics;

namespace BitcoinFinder
{
    public class PartitionBigInteger
    {
        public static IEnumerable<(BigInteger, BigInteger)> Create(BigInteger min, BigInteger max, int partitionCount = 0)
        {
            if (partitionCount <= 0)
            {
                partitionCount = Environment.ProcessorCount;
            }

            BigInteger chunkSize = (max - min + 1) / partitionCount;

            for (int i = 0; i < partitionCount - 1; i++)
            {
                BigInteger chunkStart = min + (i * chunkSize);
                yield return (chunkStart, chunkStart + chunkSize - 1);
            }

            yield return (min + ((partitionCount - 1) * chunkSize), max);
        }
    }
}