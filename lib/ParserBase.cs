using System.Globalization;

namespace Smart.Parser.Lib
{
    public class ParserBase
    {
        public static bool UseDecimalRawNormalization = false;
        public static NumberFormatInfo ParserNumberFormatInfo = new NumberFormatInfo();

        public ParserBase() => ParserNumberFormatInfo.NumberDecimalSeparator = ",";

        public static string NormalizeRawDecimalForTest(string s)
        {
            if (!UseDecimalRawNormalization)
            {
                return s;
            }

            return double.TryParse(s, out var v) ? v.ToString(ParserNumberFormatInfo) : s.Replace(".", ",").Replace("\u202f", " ");
        }
    }
}
