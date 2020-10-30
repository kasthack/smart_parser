using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TI.Declarator.ParserCommon
{
    public static class TextHelpers
    {
        private static readonly CultureInfo RussianCulture = CultureInfo.CreateSpecificCulture("ru-ru");

        public static decimal ParseDecimalValue(this string val)
        {
            var processedVal = Regex.Replace(val, @"\s+", "");
            return !decimal.TryParse(processedVal, NumberStyles.Any, RussianCulture, out var res) && !decimal.TryParse(processedVal, NumberStyles.Any, CultureInfo.InvariantCulture, out res)
                ? throw new Exception("can't parse value '" + processedVal + "' as decimal")
                : res;
        }

        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        /// <summary>
        /// Extracts a four-digit representation of year from given string
        /// and converts it to an integer
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static readonly Regex ExtractYearRegex = new Regex("[0-9]{4}", RegexOptions.Compiled);

        public static int? ExtractYear(string str)
        {
            var m = ExtractYearRegex.Match(str);
            if (m.Success)
            {
                var year = int.Parse(m.Groups[0].Value);
                return year > DateTime.Today.Year || year < 1980 ? null : (int?)year;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Replaces Latin characters that accidentally found their way into Russian words
        /// with their Cyrillic counterparts. Use with caution.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveStupidTranslit(this string str) => str.Replace('A', 'А').Replace('a', 'а')
                .Replace('C', 'С').Replace('c', 'с')
                .Replace('E', 'Е').Replace('e', 'е')
                .Replace('M', 'М')
                .Replace('O', 'О').Replace('o', 'о')
                .Replace('P', 'Р').Replace('p', 'р')
                .Replace('T', 'Т')
                .Replace('X', 'Х').Replace('x', 'х');

        public static string ReplaceEolnWithSpace(this string str) => str.Replace('\n', ' ').Trim();

        public static string CoalesceWhitespace(this string str) => Regex.Replace(str, "[ ]+", " ");

        public static string NormSpaces(this string str) => str.ReplaceEolnWithSpace().CoalesceWhitespace();

        public static string ReplaceFirst(this string str, string substr, string replStr)
        {
            var replRegex = new Regex(Regex.Escape(substr));
            return replRegex.Replace(str, replStr, 1);
        }

        public static bool CanBeInitials(string s) => Regex.Match(s.Trim(), @"\w\.\w\.").Success;

        public static bool CanBePatronymic(string s)
        {
            s = s.Replace("-", "");
            if (s.Length == 0)
            {
                return false;
            }

            return !char.IsUpper(s[0])
                ? false
                : s.EndsWith("вич") ||
                   s.EndsWith("вна") ||
                   s.EndsWith("вной") ||
                   s.EndsWith("внва") ||
                   s.EndsWith("вны") ||
                   (s.Length <= 4 && s.EndsWith(".")) || // "В." "В.П." "Вяч."
                   s.EndsWith("тич") ||
                   s.EndsWith("мич") ||
                   s.EndsWith("ьич") ||
                   s.EndsWith("ьича") ||
                   s.EndsWith("ьича") ||
                   s.EndsWith("вича") ||
                   s.EndsWith("тича") ||
                   s.EndsWith("мича") ||
                   s.EndsWith("чны") ||
                   s.EndsWith("чна") ||
                   s.EndsWith("ьичем") ||
                   s.EndsWith("тичем") ||
                   s.EndsWith("мичем") ||
                   s.EndsWith("вичем") ||
                   s.EndsWith("чной") ||
                   s.EndsWith("вной");
        }

        public static bool MayContainsRole(string s)
        {
            s = s.OnlyRussianLowercase();
            return s.Length == 0
                ? false
                : s.Contains("заместител") ||
                   s.Contains("начальник") ||
                   s.Contains("аудитор") ||
                   s.Contains("депутат") ||
                   s.Contains("секретарь") ||
                   s.Contains("уполномоченный") ||
                   s.Contains("председатель") ||
                   s.Contains("бухгалтер") ||
                   s.Contains("руководител");
        }
    }
}