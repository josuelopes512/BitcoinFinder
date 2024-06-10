using Newtonsoft.Json;
using System.Numerics;


namespace BitcoinFinder
{
    public class Range
    {
        public string Min { get; set; }
        public string Max { get; set; }
    }

    public class RangeConvert
    {
        public static List<Range> ranges = JsonConvert.DeserializeObject<List<Range>>(File.ReadAllText("C:\\Users\\maste\\source\\repos\\BitcoinFinder\\BitcoinFinder\\ranges.json"));

        public List<(BigInteger min, BigInteger max)> rangesList = new List<(BigInteger min, BigInteger max)>
        {

        };

        public RangeConvert()
        {
            rangesList.AddRange(ranges.Select(r => (Utils.ParseHex(r.Min), Utils.ParseHex(r.Max))).ToList());
        }
    }
}
