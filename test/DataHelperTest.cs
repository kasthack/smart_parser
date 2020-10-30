using Microsoft.VisualStudio.TestTools.UnitTesting;
using TI.Declarator.ParserCommon;
using Smart.Parser.Lib;

namespace test
{
    [TestClass]
    public class DataHelperTest
    {
        public DataHelperTest()
        {
        }

        [TestMethod]
        public void TestParseSquare()
        {
            const string square = "доля 1/1876 от 802898980";
            var result = DataHelper.ParseSquare(square);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestOnlyRussianRegex()
        {
            const string s = "годо-вой дох-од в 2019 –()";
            var result = s.OnlyRussianLowercase();
            Assert.AreEqual("годовойдоходв", result);
        }

        [TestMethod]
        public void TestParseNames()
        {
            var result = DataHelper.IsPublicServantInfo(" Санжицыренова Н.Ж.-Д.");
            Assert.IsTrue(result);
            result = DataHelper.IsPublicServantInfo("Блохин В.");
            Assert.IsTrue(result);

            result = DataHelper.IsPublicServantInfo("Ибрагимов С.-Э.С.-А.");
            Assert.IsTrue(result);

            result = DataHelper.IsPublicServantInfo("ВИЛИСОВА ГАЛИНА ИВАНОВНА");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestPublicServantInfo()
        {
            var result = DataHelper.IsPublicServantInfo("ребенок");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestReadSquareAndCountry()
        {
            var s = "1 Россия";
            decimal square = -1;
            DataHelper.ReadSquareAndCountry(s, out square, out var country);
            Assert.IsTrue(square == 1);
            Assert.IsTrue(country == "Россия");

            s = "(Россия)";
            square = -1;
            DataHelper.ReadSquareAndCountry(s, out square, out country);
            Assert.IsTrue(square == -1);
        }

        [TestMethod]
        public void TestParseDocumentFileName()
        {
            const string file1 = "min_agr_new/2013/9037/dep_gos_slyzhbi_2013.xls";
            const string file2 = "Ministries/min_agr_new/2014/30202.xls";
            const string file3 = "Test Samples/2577/6.docx";

            var result = DataHelper.ParseDocumentFileName(file1, out var id, out var archive_file);
            Assert.IsTrue(result);
            Assert.AreEqual(9037, id.Value);
            Assert.AreEqual("dep_gos_slyzhbi_2013.xls", archive_file);

            result = DataHelper.ParseDocumentFileName(file2, out id, out archive_file);
            Assert.IsTrue(result);
            Assert.AreEqual(30202, id.Value);
            Assert.AreEqual(null, archive_file);

            result = DataHelper.ParseDocumentFileName(file3, out id, out archive_file);
            Assert.IsTrue(result);
            Assert.AreEqual(2577, id.Value);
            Assert.AreEqual("6.docx", archive_file);
        }
    }
}
