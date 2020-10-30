using System;
using System.IO;
using System.Collections.Generic;
#if WIN64
using Microsoft.Office.Interop.Excel;
#endif
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

using TI.Declarator.ParserCommon;
using Parser.Lib;

namespace Smart.Parser.Adapters
{
    public class NpoiExcelAdapter : IAdapter
    {
        private XSSFWorkbook WorkBook;
        private int SheetIndex = 0;
        private readonly int SheetCount;
        private readonly Cell EmptyCell;
        private int MaxRowsToProcess;
        private readonly string TempFileName;
#if WIN64
        string ConvertFile2TempXlsX(string filename)
        {
            Application excel = new Application();
            var doc = excel.Workbooks.Open(Path.GetFullPath(filename),ReadOnly:true);
            TempFileName = Path.GetTempFileName();
            Logger.Debug(string.Format("use {0} to store temp xlsx file", TempFileName));
            excel.DisplayAlerts = false;
            doc.SaveAs(
                Filename:TempFileName,
                FileFormat: XlFileFormat.xlOpenXMLWorkbook,
                ConflictResolution: XlSaveConflictResolution.xlLocalSessionChanges,
                WriteResPassword: "");
            doc.Close();
            excel.Quit();
            excel = null;
            return TempFileName;
        }
#endif
        public override bool IsExcel() => true;

        public NpoiExcelAdapter(string fileName, int maxRowsToProcess = -1)
        {
            this.DocumentFile = fileName;
            this.TempFileName = null;
            var extension = Path.GetExtension(fileName);
#if WIN64
            if (extension == ".xls")
            {
                fileName = ConvertFile2TempXlsX(fileName);
            }
#endif
            var file = new StreamReader(Path.GetFullPath(fileName));
            this.WorkBook = new XSSFWorkbook(file.BaseStream);
            //WorkBook = new XSSFWorkbook(Path.GetFullPath(fileName));
            this.EmptyCell = new Cell();
            this.MaxRowsToProcess = maxRowsToProcess;

            this.SheetCount = this.GetWorkSheetCount(out this.SheetIndex);
            this.WorkBook.SetActiveSheet(this.SheetIndex);
            this.TrimEmptyLines();
        }

        public static IAdapter CreateAdapter(string fileName, int maxRowsToProcess = -1) => new NpoiExcelAdapter(fileName, maxRowsToProcess);

        ~NpoiExcelAdapter()
        {
            this.WorkBook = null;
            if (this.TempFileName != null)
            {
                File.Delete(this.TempFileName);
            }
        }

        public override string GetDocumentPosition(int row, int col) => this.GetDocumentPositionExcel(row, col);

        public Cell GetCell(string cellIndex)
        {
            var cellRef = new CellReference(cellIndex);
            return this.GetCell(cellRef.Row, cellRef.Col);
        }

        public override List<Cell> GetCells(int row, int maxColEnd = MaxColumnsCount)
        {
            var index = 0;
            var result = new List<Cell>();
            do
            {
                var cell = this.GetCell(row, index);
                result.Add(cell);

                index += cell.MergedColsCount;
            }
            while (index < maxColEnd);

            return result;
        }
        public class CellAddress
        {
            public int row { get; set; }
            public int column { get; set; }
            public override int GetHashCode() => (this.row * 100) + this.column; //maximal 100 columns in excel 
            public override bool Equals(object obj) => this.Equals(obj as CellAddress);
            public bool Equals(CellAddress obj) => obj != null && obj.row == this.row && obj.column == this.column;
        }
        private readonly Dictionary<CellAddress, Cell> Cache = new Dictionary<CellAddress, Cell>();
        private void InvalidateCache() => this.Cache.Clear();
        public override Cell GetCell(int row, int column)
        {
            var address = new CellAddress{row=row, column=column};
            if (this.Cache.ContainsKey(address))
            {
                return this.Cache[address];
            }
            var c = this.GetCellWithoutCache(row, column);
            this.Cache[address] = c;
            return c;
        }
        private Cell GetCellWithoutCache(int row, int column)
        {
            var defaultSheet = this.WorkBook.GetSheetAt(this.SheetIndex);
            var currentRow = defaultSheet.GetRow(row);
            if (currentRow == null)
            {
                //null if row contains only empty cells
                return this.EmptyCell;
            }
            var cell = currentRow.GetCell(column);
            if (cell == null)
            {
                return this.EmptyCell;
            }

            var isMergedCell = cell.IsMergedCell;
            int firstMergedRow;
            int mergedRowsCount;
            int mergedColsCount;
            if (isMergedCell)
            {
                var mergedRegion = this.GetMergedRegion(defaultSheet, cell);
                firstMergedRow = mergedRegion.FirstRow;
                mergedRowsCount = mergedRegion.LastRow - mergedRegion.FirstRow + 1;
                mergedColsCount = mergedRegion.LastColumn - mergedRegion.FirstColumn + 1;
            }
            else
            {
                firstMergedRow = cell.RowIndex;
                mergedRowsCount = 1;
                mergedColsCount = 1;
            }

            var cellContents = cell.ToString();
            var cellWidth = 0;
            for (var i = 0; i < mergedColsCount; i++)
            {
                cellWidth += defaultSheet.GetColumnWidth(column + i);
                //   to do npoi
            }

            return new Cell
            {
                IsMerged = isMergedCell,
                FirstMergedRow = firstMergedRow,
                MergedRowsCount = mergedRowsCount,
                MergedColsCount = mergedColsCount,
                // FIXME to init this property we need a formal definition of "header cell"
                IsEmpty = cellContents.IsNullOrWhiteSpace(),
                Text = cellContents,
                Row = row,
                Col = column,
                CellWidth = cellWidth
            };
        }

        private void TrimEmptyLines()
        {
            var row = this.GetRowsCount() - 1;
            while (row >= 0 && this.IsEmptyRow(row)) {
                this.MaxRowsToProcess = row;
                row--;
            }
        }

        public override int GetRowsCount()
        {
            var rowCount = this.WorkBook.GetSheetAt(this.SheetIndex).PhysicalNumberOfRows;
            return this.MaxRowsToProcess != -1 ? Math.Min(this.MaxRowsToProcess, rowCount) : rowCount;
        }

        public override int GetColsCount()
        {
            var firstSheet = this.WorkBook.GetSheetAt(this.SheetIndex);

            // firstSheet.GetRow(0) can fail, we have to use enumerators
            var iter = firstSheet.GetRowEnumerator();
            iter.MoveNext();
            var firstRow = (IRow)iter.Current;
            var firstLineColsCount = firstRow.Cells.Count;
            return firstLineColsCount;
        }

        private CellRangeAddress GetMergedRegion(ISheet sheet, ICell cell)
        {
            for (var i = 0; i < sheet.NumMergedRegions; i++)
            {
                var region = sheet.GetMergedRegion(i);
                if (region.FirstRow <= cell.RowIndex && cell.RowIndex <= region.LastRow &&
                    region.FirstColumn <= cell.ColumnIndex && cell.ColumnIndex <= region.LastColumn)
                {
                    return region;
                }
            }

            throw new Exception($"Could not find merged region containing cell at row#{cell.RowIndex}, column#{cell.ColumnIndex}");
        }
        public override int GetWorkSheetCount() => this.GetWorkSheetCount(out var curIndex);

        public int GetWorkSheetCount(out int curSheetIndex)
        {
            var worksheetCount = 0;
            curSheetIndex = -1;
            for (var index = 0; index < this.WorkBook.NumberOfSheets; index++)
            {
                var hidden = this.WorkBook.IsSheetHidden(index);
                var ws = this.WorkBook[index];
                if (!hidden && ws.LastRowNum > 0)
                {
                    if (curSheetIndex < 0)
                    {
                        curSheetIndex = 0;
                    }

                    worksheetCount++;
                }
            }
            if (worksheetCount == 0)
            {
                throw new Exception(string.Format("Excel sheet {0} has no visible worksheets", this.DocumentFile));
            }

            this.SheetIndex = curSheetIndex;
            this.InvalidateCache();
            return worksheetCount;
        }

        public override void SetCurrentWorksheet(int index)
        {
            var count = 0;
            var i = 0;
            var found = false;
            for ( ; i < this.WorkBook.NumberOfSheets; i++)
            {
                var hidden = this.WorkBook.IsSheetHidden(i);
                var ws = this.WorkBook[i];

                if (!hidden && ws.LastRowNum > 0)
                {
                    if (count == index)
                    {
                        this.SheetIndex = i;
                        found = true;
                        break;
                    }
                    count++;
                }
            }
            if (!found)
            {
                throw new SmartParserException("wrong  sheet index");
            }
            this.WorkBook.SetActiveSheet(this.SheetIndex);
            this.InvalidateCache();
        }

        public override string GetWorksheetName() => this.WorkBook.GetSheetName(this.SheetIndex);

        public override int? GetWorksheetIndex() => this.SheetCount == 1 ? null : (int?)this.SheetIndex;
    }
}
