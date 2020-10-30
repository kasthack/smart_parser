using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Smart.Parser.Adapters;

namespace test
{
    [TestClass]
    public class XlsxTest
    {
        [TestMethod]
        public void XlsxTypeCTest()
        {
            var xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "fsin_2016_extract.xlsx");
            var adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile);
            //Parser parser = new Parser(adapter);

            //parser.Process();
        }
    }
}
