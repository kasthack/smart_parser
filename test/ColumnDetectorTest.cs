using Microsoft.VisualStudio.TestTools.UnitTesting;
using Smart.Parser.Adapters;
using TI.Declarator.ParserCommon;
using System.IO;
using Smart.Parser.Lib;
using Parser.Lib;

namespace test
{
    [TestClass]
    public class ColumnDetectorTest
    {
        [TestMethod]
        public void ColumnDetectorTest1()
        {
            var xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "fsin_2016_extract.xlsx");
            var adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile);

            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(ordering.ColumnOrder.Count, 12);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.NameOrRelativeType].BeginColumn == 0);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.Occupation].BeginColumn == 1);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateType].BeginColumn == 2);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateOwnershipType].BeginColumn == 3);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateSquare].BeginColumn == 4);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateCountry].BeginColumn == 5);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertyType].BeginColumn == 6);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertySquare].BeginColumn == 7);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertyCountry].BeginColumn == 8);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.Vehicle].BeginColumn == 9);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.DeclaredYearlyIncome].BeginColumn == 10);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.DataSources].BeginColumn == 11);
        }

        [TestMethod]
        public void EmptyRealStateTypeColumnDetectorTest1()
        {
            var xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "rabotniki_podved_organizacii_2013.xlsx");
            var adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile);
            ColumnPredictor.InitializeIfNotAlready();
            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.Number].BeginColumn == 0);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.NameOrRelativeType].BeginColumn == 1);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.Occupation].BeginColumn == 2);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateType].BeginColumn == 3);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateOwnershipType].BeginColumn == 4);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateSquare].BeginColumn == 5);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateCountry].BeginColumn == 6);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertyType].BeginColumn == 7);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertySquare].BeginColumn == 8);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertyCountry].BeginColumn == 9);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.Vehicle].BeginColumn == 10);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.DeclaredYearlyIncome].BeginColumn == 11);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.DataSources].BeginColumn == 12);
        }

        [TestMethod]
        public void ColumnDetectorTest1TIAdapter()
        {
            var xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "fsin_2016_extract.xlsx");

            //IAdapter adapter = NpoiExcelAdapter.CreateAdapter(xlsxFile);
            // aspose do not want to read column widthes from this file, use aspose
            // fix it in the future (is it a bug in Npoi library?).  

            var adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile);

            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(ordering.ColumnOrder.Count, 12);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.NameOrRelativeType].BeginColumn == 0);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.Occupation].BeginColumn == 1);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateType].BeginColumn == 2);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateOwnershipType].BeginColumn == 3);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateSquare].BeginColumn == 4);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.OwnedRealEstateCountry].BeginColumn == 5);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertyType].BeginColumn == 6);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertySquare].BeginColumn == 7);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.StatePropertyCountry].BeginColumn == 8);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.Vehicle].BeginColumn == 9);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.DeclaredYearlyIncome].BeginColumn == 10);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.DataSources].BeginColumn == 11);
        }

        [TestMethod]
        public void RealEstateColumnDetector()
        {
            var docxFile = Path.Combine(TestUtil.GetTestDataPath(), "glav_44_2010.doc");
            var adapter = OpenXmlWordAdapter.CreateAdapter(docxFile, -1);

            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(ordering.ColumnOrder.Count, 9);
        }

        [TestMethod]
        public void FixVehicleColumns()
        {
            var xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "17497.xls");
            var adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile, -1);
            ColumnPredictor.InitializeIfNotAlready();

            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(15, ordering.ColumnOrder.Count);
            Assert.IsTrue(ordering.ContainsField(DeclarationField.VehicleType));
            Assert.IsTrue(ordering.ContainsField(DeclarationField.VehicleModel));
            Assert.IsFalse(ordering.ContainsField(DeclarationField.Vehicle));
        }

        [TestMethod]
        public void RedundantColumnDetector()
        {
            var docxFile = Path.Combine(TestUtil.GetTestDataPath(), "18664.docx");
            var adapter = OpenXmlWordAdapter.CreateAdapter(docxFile, -1);

            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(ordering.ColumnOrder.Count, 13);
            Assert.AreEqual(ordering.ColumnOrder[DeclarationField.AcquiredProperty].BeginColumn, 11);
            Assert.AreEqual(ordering.ColumnOrder[DeclarationField.MoneySources].BeginColumn, 12);
        }

        [TestMethod]
        public void TwoRowHeaderEmptyTopCellTest()
        {
            var docxFile = Path.Combine(TestUtil.GetTestDataPath(), "57715.doc");
            var adapter = OpenXmlWordAdapter.CreateAdapter(docxFile, -1);

            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(ordering.ColumnOrder.Count, 13);
            Assert.AreEqual(ordering.ColumnOrder[DeclarationField.Vehicle].BeginColumn, 10);
            Assert.AreEqual(ordering.ColumnOrder[DeclarationField.DeclaredYearlyIncome].BeginColumn, 11);
        }

        [TestMethod]
        public void SpendingsWrongColumnTest()
        {
            var docxFile = Path.Combine(TestUtil.GetTestDataPath(), "82442.doc");
            var adapter = OpenXmlWordAdapter.CreateAdapter(docxFile, -1);

            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(ordering.ColumnOrder[DeclarationField.DeclaredYearlyIncome].BeginColumn, 1);
        }

        [TestMethod]
        public void TwoRowHeaderEmptyTopCellTest2()
        {
            var xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "customs-tworow-header.xls");
            var adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile);

            ColumnPredictor.InitializeIfNotAlready();
            var ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(ordering.ColumnOrder.Count, 14);
            Assert.AreEqual(ordering.ColumnOrder[DeclarationField.Occupation].BeginColumn, 2);
        }
    }
}
