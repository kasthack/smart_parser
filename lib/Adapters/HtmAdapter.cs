using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using AngleSharp.Dom;
using Smart.Parser.Lib.Adapters.HtmlSchemes;

namespace Smart.Parser.Adapters
{
    public class WorksheetInfo
    {
        public WorksheetInfo()
        {
        }

        public WorksheetInfo(string personName, string year, string title, List<List<Cell>> table)
        {
            this.PersonName = personName;
            this.Year = year;
            this.Title = title;
            this.Table = table;
        }

        public string PersonName { get; set; }
        public string Year { get; set; }
        public string Title { get; set; }
        public List<List<Cell>> Table { get; set; }
    }

    public class HtmAdapter : IAdapter
    {
        #region consts
        protected const string NAME_COLUMN_CAPTION = "ФИО";
        protected const string REAL_ESTATE_CAPTION = "Вид недвижимости в собственности";
        protected const string REAL_ESTATE_SQUARE = "Площадь в собственности (кв.м)";
        protected const string REAL_ESTATE_OWNERSHIP = "Вид собственности";
        protected static List<IHtmlScheme> _allSchemes = new List<IHtmlScheme>()
            {
                new ArbitrationCourt1(),
                new ArbitrationCourt2(),
            };

        #endregion

        #region fields
        protected List<WorksheetInfo> _worksheets;
        protected int _worksheetIndex;
        protected IHtmlScheme _scheme;
        #endregion

        #region properties
        public WorksheetInfo Worksheet => this._worksheets[this._worksheetIndex];
        #endregion

        public HtmAdapter(string filename)
        {
            this.DocumentFile = filename;
            using var document = AngleHtmlAdapter.GetAngleDocument(filename);
            this._scheme = _allSchemes.Find(x => x.CanProcess(document));
            this._scheme.Document = document;
            this.MakeWorksheets(document);
            this._scheme.Document = null; // free
        }

        private void MakeWorksheets(IDocument document)
        {
            this._worksheetIndex = 0;
            var years = this._scheme.GetYears();
            if (years.Count > 0)
            {
                this.MakeWorksheetWithYears(document, years);
            }
            else
            {
                this.MakeWorksheetWithoutYears(document);
            }
        }

        private void MakeWorksheetWithoutYears(IDocument document)
        {
            this._worksheets = new List<WorksheetInfo>(1);
            var worksheet = new WorksheetInfo();
            this.MakeTable(document, worksheet);
            this._worksheets.Add(worksheet);
        }

        private void MakeWorksheetWithYears(IDocument document, List<int> years)
        {
            this._worksheets = new List<WorksheetInfo>(years.Count);
            foreach (var year in years)
            {
                var currWorksheet = new WorksheetInfo();
                this.MakeTable(document, currWorksheet, year.ToString());
                this._worksheets.Add(currWorksheet);
            }
        }

        protected  void MakeTable(IDocument document, WorksheetInfo worksheet, string year = null)
        {
            var table = this.GetTable(document, year, out var name, out var title);
            worksheet.PersonName = name;
            worksheet.Table = table;
            worksheet.Year = year;
            worksheet.Title = title;
        }

        protected  List<List<Cell>> GetTable(IDocument document,  string year, out string name,  out string title)
        {
            name = this._scheme.GetPersonName();
            title = this._scheme.GetTitle( year);
            var members = this._scheme.GetMembers( name, year);

            var table = new List<List<Cell>>
            {
                this.MakeHeaders(members.First(), 1).ToList()
            };
            this.ProcessMainMember(table, members.Skip(0).First(), name);
            this.ProcessAdditionalMembers(table, members.Skip(1), name);
            table.Insert(0, GetTitleRow(title, table));
            return table;
        }

        private static List<Cell> GetTitleRow(string title, List<List<Cell>> table)
        {
            var titleRow = new List<Cell>();
            var titleCell = new Cell
            {
                IsMerged = true,
                Text = title,
                Row = 0,
                Col = 0,
                MergedColsCount = table[1].Count,
                MergedRowsCount = 1
            };
            titleRow.Add(titleCell);
            return titleRow;
        }

        protected void ProcessAdditionalMembers(List<List<Cell>> table, IEnumerable<IElement> members, string declarantName)
        {
            foreach(var memberElement in members)
            {
                var name = this._scheme.GetMemberName(memberElement);
                var tableLines = ExtractLinesFromTable(this._scheme.GetTableFromMember(memberElement));
                this._scheme.ModifyLinesForAdditionalFields(tableLines);

                for (var i = 1; i < tableLines.Count; i++)
                {
                    var line = new List<Cell>
                    {
                        GetCell(name, table.Count, 0)
                    };
                    //ModifyLinesForRealEstate(tableLines);
                    line.AddRange(GetRow(tableLines[i], table.Count, 1));
                    table.Add(line);
                }
            }
        }

        protected void ProcessMainMember(List<List<Cell>> table, IElement memberElement, string name)
        {
            var tableLines = ExtractLinesFromTable(this._scheme.GetTableFromMember(memberElement));
            this._scheme.ModifyLinesForAdditionalFields(tableLines, true);
            foreach (var tableLine in tableLines.Skip(1))
            {
                var line = new List<Cell>();
                table.Add(line);
                if (table.Count > 2)
                {
                    name = "";
                }

                line.Add(GetCell(name, table.Count, 0));
                line.AddRange(GetRow(tableLine, table.Count, 1));
            }
        }

        protected IEnumerable<Cell> MakeHeaders( IElement memberElement, int rowNum)
        {
            var lines = ExtractLinesFromTable(this._scheme.GetTableFromMember(memberElement));
            var headerLine = lines[0];
            this._scheme.ModifyHeaderForAdditionalFields(headerLine);
            headerLine.Insert(0, NAME_COLUMN_CAPTION);
            return GetRow(headerLine, rowNum);
        }

        protected static List<List<string>> ExtractLinesFromTable(IElement tableElement)
        {
            var lines = new List<List<string>>();
            var linesSelection = tableElement.QuerySelectorAll("tr");
            foreach(var lineElement in linesSelection)
            {
                var splitedCellsLine = new List<List<string>>();
                foreach(var cell in lineElement.Children)
                {
                    var splitted = new List<string>();
                    var current = "";

                    foreach (var child in cell.ChildNodes)
                    {
                        if (child.NodeName == "BR")  {
                            splitted.Add(current);
                            current = "";
                        } else if (child.NodeName == "P") {
                            splitted.Add(child.TextContent.Replace("\n", " ").Replace("\t", "").Trim());
                        } else  {
                            current += child.TextContent.Replace("\n", " ").Replace("\t", "").Trim();
                        }
                    }
                    splitted.Add(current);
                    splitedCellsLine.Add(splitted);
                }

                var finish = false;
                while (!finish)
                {
                    finish = true;
                    var line = new List<string>();
                    foreach (var cellList in splitedCellsLine)
                    {
                        if (cellList.Count > 0)
                        {
                            finish = false;
                            var item = cellList[0];
                            cellList.RemoveAt(0);
                            line.Add(item);
                        }
                        else
                        {
                            line.Add("");
                        }
                    }
                    if (finish)
                    {
                        break;
                    }

                    lines.Add(line);
                }
            }
            return lines;
        }

        public static bool CanProcess(string filename)
        {
            var document = AngleHtmlAdapter.GetAngleDocument(filename);
            return _allSchemes.Any(x=>x.CanProcess(document));
        }

        protected static Cell GetCell(string text, int row, int column)
        {
            var cell = new Cell
            {
                Text = text,
                Row = row,
                Col = column,
                IsMerged = false,
                CellWidth = 1
            };
            return cell;
        }

        private static IEnumerable<Cell> GetRow(List<string> tableLine, int row, int columnShift = 0) => tableLine.Select((x, i) => GetCell(x, row, i + columnShift));

        #region IAdapter
        public override Cell GetCell(int row, int column)
        {
            var cell = this.Worksheet.Table[row][column];
            return cell;
        }

        public override int GetColsCount() => this.Worksheet.Table[1].Count;

        public override int GetRowsCount() => this.Worksheet.Table.Count;

        public override bool IsExcel() => false;

        public override List<Cell> GetCells(int row, int maxColEnd = 1024) => this.Worksheet.Table[row];

        public override string GetTitleOutsideTheTable() => this.Worksheet.Title;

        public override int GetWorkSheetCount() => this._worksheets.Count;

        public override int GetTablesCount() => 1;

        public override void SetCurrentWorksheet(int sheetIndex) => this._worksheetIndex = sheetIndex;

        public override int? GetWorksheetIndex() => this._worksheetIndex;
        #endregion
    }
}
