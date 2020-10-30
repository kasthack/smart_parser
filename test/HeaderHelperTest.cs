using Microsoft.VisualStudio.TestTools.UnitTesting;
using Smart.Parser.Adapters;
using TI.Declarator.ParserCommon;
using System.IO;
using Smart.Parser.Lib;
using static Algorithms.LevenshteinDistance;

namespace test
{
    [TestClass]
    public class HeaderHelperTest
    {
        [TestMethod]
        public void HeaderHelperTest1()
        {
            var docFile = Path.Combine(TestUtil.GetTestDataPath(), "E - min_sport_2012_Rukovoditeli_gospredpriyatij,_podvedomstvennyih_ministerstvu.doc");
            // IAdapter adapter = AsposeExcelAdapter.CreateAsposeExcelAdapter(xlsxFile);
            var adapter = AsposeDocAdapter.CreateAdapter(docFile);
        }

        [TestMethod]
        public void StringComparisonTest()
        {
            const string s1 = "собствен-ности";
            const string s2 = "собственности";
            var result = Calculate(s1, s2);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TryGetFieldTest()
        {
            const string s1 = "N№ п/п";
            Assert.IsTrue(s1.IsNumber());
        }

        [TestMethod]
        public void HeaderDetectionTest()
        {
            var big_header = "Объекты недвижимости, находящиеся в собственности Вид\nсобствен\nности";
            var field = HeaderHelpers.GetField(big_header);

            big_header = "Объекты недвижимости имущества находящиеся в пользовании Вид обьекта";
            field = HeaderHelpers.GetField(big_header);
        }

        [TestMethod]
        public void TestSwapCountryAndSquare()
        {
            var square = "рф";
            var country = "57 кв м";
            RealtyParser.SwapCountryAndSquare(ref square, ref country);
            Assert.AreEqual("рф", country);
            Assert.AreEqual("57 кв м", square);

            // no swap
            RealtyParser.SwapCountryAndSquare(ref square, ref country);
            Assert.AreEqual("57 кв м", square);
        }
    }
}
