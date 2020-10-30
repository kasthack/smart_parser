using Microsoft.VisualStudio.TestTools.UnitTesting;

using Smart.Parser.Lib;

using TI.Declarator.ParserCommon;

namespace test
{
    [TestClass]
    public class TestJsonWriter
    {
        [TestMethod]
        public void TestColumnOrderJson()
        {
            var co = new ColumnOrdering();
            var s = new TColumnInfo
            {
                Field = DeclarationField.NameOrRelativeType
            };
            co.Add(s);
            JsonWriter.WriteJson("co.json", co);
        }
    }
}
