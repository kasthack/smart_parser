﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Smart.Parser.Adapters;
using TI.Declarator.ParserCommon;
using System.IO;
using Smart.Parser.Lib;

namespace test
{
    [TestClass]
    public class ColumnDetectorTest
    {
        [TestMethod]
        public void ColumnDetectorTest1()
        {
            string xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "fsin_2016_extract.xlsx");
            IAdapter adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile);

            ColumnOrdering ordering = ColumnDetector.ExamineTableBeginning(adapter);
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
            string xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "rabotniki_podved_organizacii_2013.xlsx");
            IAdapter adapter = AsposeExcelAdapter.CreateAdapter(xlsxFile);

            ColumnOrdering ordering = ColumnDetector.ExamineTableBeginning(adapter);
            Assert.AreEqual(ordering.ColumnOrder.Count, 12);
            Assert.IsTrue(ordering.ColumnOrder[DeclarationField.NameOrRelativeType].BeginColumn == 1);
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
            string xlsxFile = Path.Combine(TestUtil.GetTestDataPath(), "fsin_2016_extract.xlsx");
            IAdapter adapter = NpoiExcelAdapter.CreateAdapter(xlsxFile);

            ColumnOrdering ordering = ColumnDetector.ExamineTableBeginning(adapter);
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
    }
}
