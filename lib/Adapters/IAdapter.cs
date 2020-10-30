using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CsvHelper;
using TI.Declarator.ParserCommon;
using Parser.Lib;
using Smart.Parser.Lib.Adapters.DocxSchemes;

namespace Smart.Parser.Adapters
{
    public abstract class IAdapter : TSectionPredicates
    {
        // some excel files contain 32000 columns, most of them are empty
        // we try to found real column number in the header, by default is 1024
        public const int MaxColumnsCount = 1024;

        // specific scheme to parse tables
        public IAdapterScheme CurrentScheme = null;

        public static string ConvertedFileStorageUrl = string.Empty;

        public virtual bool IsExcel() => false;

        public virtual string GetDocumentPosition(int row, int col) => null;

        public string GetDocumentPositionExcel(int row, int col) => "R" + (row + 1).ToString() + "C" + (col + 1).ToString();

        public abstract Cell GetCell(int row, int column);

        public virtual List<Cell> GetCells(int row, int maxColEnd = MaxColumnsCount) => throw new NotImplementedException();

        public DataRow GetRow(ColumnOrdering columnOrdering, int row) => new DataRow(this, columnOrdering, row);

        // напрямую используется, пока ColumnOrdering еще не построен
        // во всех остальных случаях надо использовать Row.GetDeclarationField
        public virtual Cell GetDeclarationFieldWeak(ColumnOrdering columnOrdering, int row, DeclarationField field, out TColumnInfo colSpan)
        {
            if (!columnOrdering.ColumnOrder.TryGetValue(field, out colSpan))
            {
                throw new SmartParserFieldNotFoundException($"Field {field.ToString()} not found, row={row}");
            }

            var exactCell = this.GetCell(row, colSpan.BeginColumn);
            if (exactCell == null)
            {
                var rowData = this.GetCells(row);
                throw new SmartParserFieldNotFoundException($"Field {field} not found, row={row}, col={colSpan.BeginColumn}. Row.Cells.Count = {rowData.Count}");
            }

            return exactCell;
        }

        public abstract int GetRowsCount();

        public abstract int GetColsCount();

        public virtual string GetTitleOutsideTheTable() => throw new NotImplementedException();

        public string DocumentFile { get; set; }

        public virtual int GetWorkSheetCount() => 1;

        public virtual int GetTablesCount() => this.GetWorkSheetCount();

        public virtual void SetCurrentWorksheet(int sheetIndex) => throw new NotImplementedException();

        public virtual string GetWorksheetName() => null;

        public bool IsEmptyRow(int rowIndex)
        {
            foreach (var cell in this.GetCells(rowIndex))
            {
                if (!cell.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetUnmergedColumnsCountByFirstRow()
        {
            if (this.GetRowsCount() == 0)
            {
                return -1;
            }

            var sum = 0;
            foreach (var c in this.GetCells(0))
            {
                sum += c.MergedColsCount;
            }

            return sum;
        }

        public static int FindMergedCellByColumnNo<T>(List<List<T>> tableRows, int row, int column) where T : Cell
        {
            var r = tableRows[row];
            var sumspan = 0;
            for (var i = 0; i < r.Count; ++i)
            {
                var span = r[i].MergedColsCount;
                if ((column >= sumspan) && (column < sumspan + span))
                {
                    return i;
                }

                sumspan += span;
            }

            return -1;
        }

        protected static List<List<T>> DropDayOfWeekRows<T>(List<List<T>> tableRows) where T : Cell
        {
            var daysOfWeek = new List<string> { "пн", "вт", "ср", "чт", "пт", "сб", "вс" };
            return tableRows.TakeWhile(x => !x.All(y => daysOfWeek.Contains(y.Text.ToLower().Trim()))).ToList();
        }

        protected static bool CheckNameColumnIsEmpty<T>(List<List<T>> tableRows, int start) where T : Cell
        {
            if (tableRows.Count - start < 3)
            {
                return false; // header only
            }

            var nameInd = tableRows[start].FindIndex(x => x.Text.Length < 100 && x.Text.IsName());
            if (nameInd == -1)
            {
                return false;
            }

            for (var i = start + 1; i < tableRows.Count; ++i)
            {
                if (nameInd < tableRows[i].Count && !tableRows[i][nameInd].IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }

        protected static void MergeRow<T>(List<T> row1, List<T> row2) where T : Cell
        {
            for (var i = 0; i < row1.Count; ++i)
            {
                row1[i].Text += "\n" + row2[i].Text;
            }
        }

        public class TJsonCell
        {
            public int mc;
            public int mr;
            public int r;
            public int c;
            public string t;
        }

        public class TJsonTablePortion
        {
            public string Title;
            public string InputFileName;
            public int DataStart;
            public int DataEnd;
            public List<List<TJsonCell>> Header = new List<List<TJsonCell>>();
            public List<List<TJsonCell>> Section = new List<List<TJsonCell>>();
            public List<List<TJsonCell>> Data = new List<List<TJsonCell>>();
        }

        private List<TJsonCell> GetJsonByRow(List<Cell> row)
        {
            var outputList = new List<TJsonCell>();
            foreach (var c in row)
            {
                var jc = new TJsonCell
                {
                    mc = c.MergedColsCount,
                    mr = c.MergedRowsCount,
                    r = c.Row,
                    c = c.Col,
                    t = c.Text,
                };
                outputList.Add(jc);
            }

            return outputList;
        }

        private string GetHtmlByRow(List<Cell> row, int rowIndex)
        {
            var res = $"<tr rowindex={rowIndex}>\n";
            foreach (var c in row)
            {
                if (c.FirstMergedRow != rowIndex)
                {
                    continue;
                }

                res += "\t<td";
                if (c.MergedColsCount > 1)
                {
                    res += $" colspan={c.MergedColsCount}";
                }

                if (c.MergedRowsCount > 1)
                {
                    res += $" rowspan={c.MergedRowsCount}";
                }

                var text = c.Text.Replace("\n", "<br/>");
                res += ">" + text + "</td>\n";
            }

            res += "</tr>\n";
            return res;
        }

        public TJsonTablePortion TablePortionToJson(ColumnOrdering columnOrdering, int body_start, int body_end)
        {
            var table = new TJsonTablePortion
            {
                DataStart = body_start,
            };
            var headerEnd = columnOrdering.GetPossibleHeaderEnd();
            for (var i = columnOrdering.GetPossibleHeaderBegin(); i < columnOrdering.GetPossibleHeaderEnd(); i++)
            {
                var row = this.GetJsonByRow(this.GetCells(i));
                table.Header.Add(row);
            }

            // find section before data
            for (var i = body_start; i >= headerEnd; i--)
            {
                // cannot use prevRowIsSection
                var row = this.GetCells(i);
                if (IsSectionRow(row, columnOrdering.GetMaxColumnEndIndex(), false, out var dummy))
                {
                    table.Section.Add(this.GetJsonByRow(row));
                    break;
                }
            }

            var maxRowsCount = body_end - body_start;
            table.DataEnd = body_start;
            var addedRows = 0;
            while (table.DataEnd < this.GetRowsCount() && addedRows < maxRowsCount)
            {
                if (!this.IsEmptyRow(table.DataEnd))
                {
                    table.Data.Add(this.GetJsonByRow(this.GetCells(table.DataEnd)));
                    addedRows++;
                }

                table.DataEnd++;
            }

            return table;
        }

        public void WriteHtmlFile(string htmlFileName)
        {
            using var file = new StreamWriter(htmlFileName);
            file.WriteLine("<html><table border=1>");
            for (var i = 0; i < this.GetRowsCount(); i++)
            {
                file.WriteLine(this.GetHtmlByRow(this.GetCells(i), i));
            }

            file.WriteLine("</table></html>");
        }

        public void ExportCSV(string csvFile)
        {
            var rowCount = this.GetRowsCount();
            var colCount = this.GetColsCount();

            var stream = new FileStream(csvFile, FileMode.Create);
            var writer = new StreamWriter(stream) { AutoFlush = true };

            var csv = new CsvWriter(writer);

            for (var r = 0; r < rowCount; r++)
            {
                for (var c = 0; c < colCount; c++)
                {
                    var value = this.GetCell(r, c).Text;
                    csv.WriteField(value);
                }

                csv.NextRecord();
            }

            csv.Flush();
        }

        public virtual int? GetWorksheetIndex() => null;
    }
}
