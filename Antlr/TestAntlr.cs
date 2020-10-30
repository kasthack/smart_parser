using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using SmartAntlr;
using System.Threading;
using System.Text;

namespace test
{
    [TestClass]
    public class AntlrTest
    {
        static AntlrTest()
        {
            // to print ","  for floating delimiter
            var culture = new System.Globalization.CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = culture;
        }
        private static bool FileEquals(string path1, string path2)
        {
            var text1 = File.ReadAllText(path1, Encoding.UTF8).Replace("\r","") ;
            var text2 = File.ReadAllText(path2, Encoding.UTF8).Replace("\r","");
            return text1 == text2;
        }

        public string GetTestFilesFolder()
        {
            var curDir = Directory.GetCurrentDirectory();
            while (curDir.Length > 3 && Path.GetFileName(curDir) != "Antlr")
            {
                curDir = Path.GetDirectoryName(curDir);
            }
            return curDir.Length <= 3 ? throw new Exception("cannot find folder with test_files") : Path.Join(curDir, "test_files");
        }

        private void TestCase(GeneralAntlrParserWrapper parser, string filename)
        {
            var input = Path.Join(this.GetTestFilesFolder(), filename);
            var output = input + ".result";
            var texts = AntlrCommon.ReadTestCases(input);
            AntlrCommon.WriteTestCaseResultsToFile(parser, texts, output);
            Assert.AreEqual(FileEquals(output, input + ".result.canon"), true);
        }

        [TestMethod]
        public void StrictParser() => this.TestCase(new AntlrStrictParser(), "strict.txt");

        [TestMethod]
        public void SoupParser() => this.TestCase(new AntlrSoupParser(), "soup.txt");

        [TestMethod]
        public void CountryList() => this.TestCase(new AntlrCountryListParser(), "country_list.txt");
        [TestMethod]
        public void SquareList() => this.TestCase(new AntlrSquareParser(), "square_list.txt");

        [TestMethod]
        public void SquareAndCountry()
        {
            var parser = new AntlrStrictParser
            {
                StartFromRoot = AntlrStrictParser.StartFromRootEnum.square_and_country
            };
            this.TestCase(parser, "square_and_country.txt");
        }
    }
}
