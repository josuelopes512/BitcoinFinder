using System.Collections.Concurrent;
using System.Numerics;

namespace BitcoinFinder
{
    public class BigIntegerRange : Partitioner<BigInteger>
    {
        private readonly BigInteger _fromInclusive;
        private readonly BigInteger _toExclusive;

        public BigIntegerRange(BigInteger fromInclusive, BigInteger toExclusive) : base()
        {
            _fromInclusive = fromInclusive;
            _toExclusive = toExclusive;
        }

        public override IList<IEnumerator<BigInteger>> GetPartitions(int partitionCount)
        {
            if (partitionCount < 1)
                throw new ArgumentOutOfRangeException(nameof(partitionCount));

            BigInteger rangeSize = BigInteger.Divide(BigInteger.Subtract(_toExclusive, _fromInclusive), partitionCount);
            List<IEnumerator<BigInteger>> partitions = new List<IEnumerator<BigInteger>>();

            for (int i = 0; i < partitionCount - 1; i++)
            {
                BigInteger from = BigInteger.Add(_fromInclusive, BigInteger.Multiply(rangeSize, i));
                BigInteger to = BigInteger.Add(_fromInclusive, BigInteger.Multiply(rangeSize, i + 1));
                partitions.Add(CreatePartition(from, to));
            }

            partitions.Add(CreatePartition(BigInteger.Add(_fromInclusive, BigInteger.Multiply(rangeSize, partitionCount - 1)), _toExclusive));
            return partitions;
        }

        private IEnumerator<BigInteger> CreatePartition(BigInteger from, BigInteger to)
        {
            for (BigInteger i = from; i < to; i++)
            {
                yield return i;
            }
        }

        public static (BigInteger, BigInteger)[] DivideRange(BigInteger min, BigInteger max, int numberOfBlocks)
        {
            BigInteger rangeSize = (max - min) / numberOfBlocks;
            var ranges = new (BigInteger, BigInteger)[numberOfBlocks];

            for (int i = 0; i < numberOfBlocks; i++)
            {
                BigInteger blockMin = min + i * rangeSize;
                BigInteger blockMax = (i == numberOfBlocks - 1) ? max : blockMin + rangeSize - 1;
                ranges[i] = (blockMin, blockMax);
            }

            return ranges;
        }

        public static List<(BigInteger min, BigInteger max)> DivideRangeList(BigInteger min, BigInteger max, int numberOfBlocks = 1000)
        {
            BigInteger rangeSize = (max - min) / numberOfBlocks;

            List<(BigInteger min, BigInteger max)> ranges = new List<(BigInteger min, BigInteger max)>();

            for (int i = 0; i < numberOfBlocks; i++)
            {
                BigInteger blockMin = min + i * rangeSize;
                BigInteger blockMax = (i == numberOfBlocks - 1) ? max : blockMin + rangeSize - 1;

                ranges.Add((blockMin, blockMax));

                //ranges[i] = (blockMin, blockMax);
            }

            return ranges;
        }
    }
}