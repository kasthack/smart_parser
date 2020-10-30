using Microsoft.VisualStudio.TestTools.UnitTesting;

using Smart.Parser.Adapters;
using Smart.Parser.Lib;

using System.IO;

using TI.Declarator.JsonSerialization;

namespace test
{
    [TestClass]
    public class XlsxAdapterTest
    {
        [TestMethod]
        public void XlsxTypeCTest()
        {
            var xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "c_sample.xlsx");
            var adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile);
            var columnOrdering = ColumnDetector.ExamineTableBeginning(adapter);
            var parser = new Smart.Parser.Lib.Parser(adapter);
            var declaration = parser.Parse(columnOrdering, false, null);
            var comments = "";
            var output = DeclarationSerializer.Serialize(declaration, ref comments);
        }
    }
}
