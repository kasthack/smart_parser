using Parser.Lib;

using System;
using System.Collections.Generic;

namespace Smart.Parser.Adapters
{
    internal class AsposeExcelCell : Cell
    {
        public AsposeExcelCell(Aspose.Cells.Cell cell, Aspose.Cells.Worksheet worksheet)
        {
            if (cell == null)
            {
                return;
            }

            this.IsEmpty = cell.Type == Aspose.Cells.CellValueType.IsNull;
            // nobody wants to know how excel represents numbers inside itself
            // for "size_raw"
            this.Text = cell.GetStringValue(Aspose.Cells.CellValueFormatStrategy.DisplayStyle);
            if (this.Text == "###")
            {
                this.Text = cell.StringValue;
            }

            this.IsMerged = cell.IsMerged;
            if (this.IsMerged)
            {
                this.FirstMergedRow = cell.GetMergedRange().FirstRow;
                this.MergedRowsCount = cell.GetMergedRange().RowCount;
                this.MergedColsCount = cell.GetMergedRange().ColumnCount;
            }
            else
            {
                this.MergedColsCount = 1;
                this.MergedRowsCount = 1;
                this.FirstMergedRow = cell.Row;
            }

            this.Row = cell.Row;
            this.Col = cell.Column;
            this.CellWidth = 0;
            for (var i = 0; i < this.MergedColsCount; i++)
            {
                // test File17207: GetColumnWidthPixel returns 45, GetColumnWidth returns 0 for the same cell
                this.CellWidth += worksheet.Cells.GetColumnWidthPixel(cell.Column + i);
            }
        }
    }

    public class AsposeExcelAdapter : IAdapter
    {
        public override bool IsExcel() => true;

        public static IAdapter CreateAdapter(string fileName, int maxRowsToProcess = -1) => new AsposeExcelAdapter(fileName, maxRowsToProcess);

        public override string GetDocumentPosition(int row, int col) => this.GetDocumentPositionExcel(row, col);

        public override Cell GetCell(int row, int column)
        {
            var cell = this.worksheet.Cells.GetCell(row, column);
            return new AsposeExcelCell(cell, this.worksheet);
        }

        public override int GetRowsCount() => this.MaxRowsToProcess != -1 ? Math.Min(this.MaxRowsToProcess, this.WorkSheetRows) : this.WorkSheetRows;

        public override int GetColsCount() => this.totalColumns;

        public override List<Cell> GetCells(int rowIndex, int maxColEnd = IAdapter.MaxColumnsCount)
        {
            var result = new List<Cell>();
            var row = this.worksheet.Cells.Rows[rowIndex];
            var firstCell = row.FirstCell;
            var lastCell = row.LastCell;
            if (lastCell == null)
            {
                return result;
            }

            for (var i = 0; i <= lastCell.Column; i++)
            {
                if (i >= maxColEnd)
                {
                    break;
                }

                var cell = row.GetCellOrNull(i);
                result.Add(new AsposeExcelCell(cell, this.worksheet));
                if (cell?.IsMerged == true && cell.GetMergedRange().ColumnCount > 1)
                {
                    i += cell.GetMergedRange().ColumnCount - 1;
                }
            }

            /*
            IEnumerator enumerator = worksheet.Cells.Rows[rowIndex].GetEnumerator();
            int range_end = -1;
            while (enumerator.MoveNext())
            {
                Aspose.Cells.Cell cell = (Aspose.Cells.Cell)enumerator.Current;
                if (cell.Column < range_end)
                {
                    index++;
                    continue;
                }

                result.Add(new AsposeExcelCell(cell));

                if (cell.IsMerged)
                {
                    int first = cell.GetMergedRange().FirstColumn;
                    int count = cell.GetMergedRange().ColumnCount;
                    range_end = first + count;
                }
                index++;
            }
            */
            return result;
        }

        public override string GetTitleOutsideTheTable() => string.Empty;

        private AsposeExcelAdapter(string fileName, int maxRowsToProcess)
        {
            this.MaxRowsToProcess = maxRowsToProcess;
            this.DocumentFile = fileName;
            this.workbook = new Aspose.Cells.Workbook(fileName);
            this.workbook.Settings.NumberDecimalSeparator = ',';
            this.workbook.Settings.NumberGroupSeparator = ' ';
            // if there are multiple worksheets it is a problem
            // generate exception if more then one non-hidden worksheet
            // worksheet = workbook.Worksheets[0];
            var wsCount = 0;
            this.worksheet = null;
            var max_rows_count = 0;
            foreach (var ws in this.workbook.Worksheets)
            {
                if (ws.IsVisible && ws.Cells.Rows.Count > 0)
                {
                    wsCount++;
                    if (this.worksheet == null || max_rows_count < ws.Cells.Rows.Count)
                    {
                        this.worksheet = ws;
                        max_rows_count = ws.Cells.Rows.Count;
                    }
                }
            }

            if (wsCount == 0)
            {
                throw new Exception($"Excel sheet {fileName} has no visible worksheets");
            }

            this.workSheetName = this.worksheet.Name;

            this.worksheetCount = wsCount;
            this.WorkSheetRows = this.worksheet.Cells.Rows.Count;
            this.totalColumns = this.worksheet.Cells.MaxColumn + 1;
        }

        public override void SetCurrentWorksheet(int sheetIndex)
        {
            var count = 0;
            this.worksheet = null;
            foreach (var ws in this.workbook.Worksheets)
            {
                if (ws.IsVisible && ws.Cells.Rows.Count > 0)
                {
                    if (count == sheetIndex)
                    {
                        this.worksheet = ws;
                        break;
                    }

                    count++;
                }
            }

            if (this.worksheet == null)
            {
                throw new SmartParserException("wrong  sheet index");
            }

            this.workSheetName = this.worksheet.Name;
            this.WorkSheetRows = this.worksheet.Cells.Rows.Count;
        }

        public override int GetWorkSheetCount() => this.worksheetCount;

        public override string GetWorksheetName() => this.workSheetName;

        public override int? GetWorksheetIndex() => this.worksheet.Index;

        private readonly Aspose.Cells.Workbook workbook;
        private Aspose.Cells.Worksheet worksheet;
        private int WorkSheetRows;
        private readonly int totalColumns;
        private readonly int worksheetCount;
        private string workSheetName;
        private readonly int MaxRowsToProcess;
    }
}
