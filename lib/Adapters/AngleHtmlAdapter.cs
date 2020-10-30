using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TI.Declarator.ParserCommon;
using AngleSharp;
using AngleSharp.Dom;

namespace Smart.Parser.Adapters
{
    internal class HtmlDocHolder
    {
        public IDocument HtmlDocument;
        public int DefaultFontSize = 10;
        public string DefaultFontName = "Times New Roman";
        public int DocumentPageSizeInPixels;

        public HtmlDocHolder(IDocument htmlDocument)
        {
            this.HtmlDocument = htmlDocument;
            this.DocumentPageSizeInPixels = 1000;
        }

        public string FindTitleAboveTheTable()
        {
            var title = string.Empty;
            var foundTable = false;
            var addedLines = new HashSet<string>();
            foreach (var p in this.HtmlDocument.All.ToList())
            {
                if (p.TextContent.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (p.TextContent.Length > 300)
                {
                    continue;
                }

                if (addedLines.Contains(p.TextContent))
                {
                    continue;
                }

                addedLines.Add(p.TextContent);
                if (p.LocalName == "h1" || p.LocalName == "h2")
                {
                    title += p.TextContent + " ";
                }
                else if ((p.LocalName == "p" || p.LocalName == "div" || p.LocalName == "span") && p.TextContent.IndexOf("декабря") != -1)
                {
                    title += p.TextContent + " ";
                }
                else
                {
                    if (p.LocalName == "table")
                    {
                        foundTable = true;
                    }

                    if (!foundTable && p.LocalName == "p")
                    {
                        title += p.TextContent + " ";
                    }
                }
            }

            return title;
        }
    }

    public class MyMarkupFormatter : IMarkupFormatter
    {
        string IMarkupFormatter.Comment(IComment comment) => string.Empty;

        string IMarkupFormatter.Doctype(IDocumentType doctype) => string.Empty;

        string IMarkupFormatter.Processing(IProcessingInstruction processing) => string.Empty;

        string IMarkupFormatter.Text(ICharacterData text) => text.Data;

        string IMarkupFormatter.OpenTag(IElement element, bool selfClosing) => element.LocalName switch
        {
            "p" => "\n\n",
            "br" => "\n",
            "span" => " ",
            _ => string.Empty,
        };

        string IMarkupFormatter.CloseTag(IElement element, bool selfClosing) => string.Empty;

        string IMarkupFormatter.Attribute(IAttr attr) => string.Empty;
    }

    internal class HtmlAdapterCell : Cell
    {
        public HtmlAdapterCell(int row, int column)
        {
            this.Row = row;
            this.Col = column;
            this.Text = string.Empty;
            this.IsEmpty = true;
            this.CellWidth = 0;
            this.MergedRowsCount = 1;
            this.MergedColsCount = 1;
        }

        public HtmlAdapterCell(HtmlDocHolder docHolder, IElement inputCell, int row, int column)
        {
            this.InitTextProperties(docHolder, inputCell);
            this.FirstMergedRow = row;
            this.MergedRowsCount = 1;
            this.MergedColsCount = 1;
            this.Row = row;
            this.Col = column;
            this.IsMerged = false;
            this.IsEmpty = this.Text.IsNullOrWhiteSpace();
            int mergedColsCount;
            if (inputCell.HasAttribute("colspan") && int.TryParse(inputCell.GetAttribute("colspan"), out mergedColsCount))
            {
                this.MergedColsCount = mergedColsCount;
                this.IsMerged = this.MergedColsCount > 1;
            }

            if (inputCell.HasAttribute("rowspan") && int.TryParse(inputCell.GetAttribute("rowspan"), out mergedColsCount))
            {
                this.MergedRowsCount = mergedColsCount;
            }

            if (inputCell.HasAttribute("width"))
            {
                var s = inputCell.GetAttribute("width");
                if (s.EndsWith("%") && double.TryParse(s[0..^1], out var width))
                {
                    this.CellWidth = (int)(docHolder.DocumentPageSizeInPixels * (width / 100.0));
                }

                if (double.TryParse(s, out width))
                {
                    this.CellWidth = (int)width;
                }
                else
                {
                    this.CellWidth = 50;
                }
            }
        }

        public HtmlAdapterCell(IAdapter.TJsonCell cell)
        {
            this.Text = cell.t;
            this.MergedColsCount = cell.mc;
            this.MergedRowsCount = cell.mr;
            this.IsEmpty = this.Text.IsNullOrWhiteSpace();
            this.Row = cell.r;
            this.Col = cell.c;
        }

        private void InitTextProperties(HtmlDocHolder docHolder, IElement inputCell)
        {
            this.FontName = string.Empty;
            this.FontSize = 0;
            var myFormatter = new MyMarkupFormatter();
            // var myFormatter = new AngleSharp.Html.PrettyMarkupFormatter();
            this.Text = inputCell.ToHtml(myFormatter);
            this.IsEmpty = this.Text.IsNullOrWhiteSpace();
            if (this.FontName == null || this.FontName?.Length == 0)
            {
                this.FontName = docHolder.DefaultFontName;
            }

            if (this.FontSize == 0)
            {
                this.FontSize = docHolder.DefaultFontSize;
            }
        }
    }

    public class AngleHtmlAdapter : IAdapter
    {
        private List<List<HtmlAdapterCell>> TableRows;
        private string Title;
        private readonly int UnmergedColumnsCount;
        private int TablesCount;

        public static IDocument GetAngleDocument(string filename)
        {
            // string text = File.ReadAllText(filenameS);
            var config = Configuration.Default;
            using var fileStream = File.Open(filename, FileMode.Open);
            var context = BrowsingContext.New(config);
            var task = context.OpenAsync(req => req.Content(fileStream));
            task.Wait();
            var document = task.Result;
            return document;
        }

        public AngleHtmlAdapter(string fileName, int maxRowsToProcess)
        {
            this.TableRows = new List<List<HtmlAdapterCell>>();
            this.DocumentFile = fileName;
            var holder = new HtmlDocHolder(GetAngleDocument(fileName));
            this.Title = holder.FindTitleAboveTheTable();
            this.CollectRows(holder, maxRowsToProcess);
            this.UnmergedColumnsCount = this.GetUnmergedColumnsCountByFirstRow();
        }

        public static IAdapter CreateAdapter(string fileName, int maxRowsToProcess) => new AngleHtmlAdapter(fileName, maxRowsToProcess);

        public override string GetTitleOutsideTheTable() => this.Title;

        private List<IElement> GetHtmlTableRows(IElement htmltable) => htmltable.QuerySelectorAll("*").Where(m => m.LocalName == "tr").ToList();

        private List<IElement> GetHtmlTableCells(IElement htmlTableRow) => htmlTableRow.QuerySelectorAll("*").Where(m => m.LocalName == "td" || m.LocalName == "th").ToList();

        private void InsertRowSpanCells(int start, int end)
        {
            if (start + 1 >= end)
            {
                return;
            }

            for (; start < end; ++start)
            {
                var firstLine = this.TableRows[start];
                for (var cellIndex = 0; cellIndex < firstLine.Count; ++cellIndex)
                {
                    if (firstLine[cellIndex].MergedRowsCount > 1 && firstLine[cellIndex].FirstMergedRow == start)
                    {
                        for (var rowIndex = start + 1; rowIndex < start + firstLine[cellIndex].MergedRowsCount; ++rowIndex)
                        {
                            if (rowIndex >= this.TableRows.Count)
                            {
                                break; // #-max-rows 100
                            }

                            var additCell = new HtmlAdapterCell(rowIndex, cellIndex)
                            {
                                FirstMergedRow = start,
                                MergedRowsCount = firstLine[cellIndex].MergedRowsCount - rowIndex,
                                CellWidth = firstLine[cellIndex].CellWidth,
                            };
                            if (cellIndex < this.TableRows[rowIndex].Count)
                            {
                                this.TableRows[rowIndex].Insert(cellIndex, additCell);
                            }
                            else
                            {
                                this.TableRows[rowIndex].Add(additCell);
                            }

                            for (var afterCellIndex = cellIndex + 1; afterCellIndex < this.TableRows[rowIndex].Count; ++afterCellIndex)
                            {
                                this.TableRows[rowIndex][afterCellIndex].Col += firstLine[cellIndex].MergedColsCount;
                            }
                        }
                    }
                }
            }
        }

        private void ProcessHtmlTable(HtmlDocHolder docHolder, IElement table, int maxRowsToProcess)
        {
            var rows = this.GetHtmlTableRows(table);
            var saveRowsCount = this.TableRows.Count;
            var maxCellsCount = 0;
            var maxSumSpan = 0;
            for (var r = 0; r < rows.Count; ++r)
            {
                var newRow = new List<HtmlAdapterCell>();
                var sumspan = 0;
                var row = rows[r];
                var isEmpty = true;
                foreach (var rowCell in this.GetHtmlTableCells(rows[r]))
                {
                    var c = new HtmlAdapterCell(docHolder, rowCell, this.TableRows.Count, sumspan);
                    newRow.Add(c);
                    for (var k = 1; k < c.MergedColsCount; ++k)
                    {
                        newRow.Add(new HtmlAdapterCell(this.TableRows.Count, sumspan + k));
                    }

                    sumspan += c.MergedColsCount;
                    isEmpty = isEmpty && c.IsEmpty;
                }

                if (isEmpty)
                {
                    continue;
                }

                maxCellsCount = Math.Max(newRow.Count, maxCellsCount);
                maxSumSpan = Math.Max(sumspan, maxSumSpan);

                // see 7007_8.html in tests
                for (var k = sumspan; k < maxSumSpan; ++k)
                {
                    newRow.Add(new HtmlAdapterCell(this.TableRows.Count, sumspan + k));
                }

                if (r == 0 && this.TableRows.Count > 0 &&
                    BigramsHolder.CheckMergeRow(
                        this.TableRows.Last().ConvertAll(x => x.Text),
                        newRow.ConvertAll(x => x.Text)))
                {
                    MergeRow(this.TableRows.Last(), newRow);
                }
                else
                {
                    this.TableRows.Add(newRow);
                }

                if ((maxRowsToProcess != -1) && (this.TableRows.Count >= maxRowsToProcess))
                {
                    break;
                }
            }

            if (saveRowsCount < this.TableRows.Count)
            {
                if (maxCellsCount <= 4)
                {
                    // remove this suspicious table
                    this.TableRows.RemoveRange(saveRowsCount, this.TableRows.Count - saveRowsCount);
                }
                else
                {
                    this.InsertRowSpanCells(saveRowsCount, this.TableRows.Count);
                    if (CheckNameColumnIsEmpty(this.TableRows, saveRowsCount))
                    {
                        this.TableRows.RemoveRange(saveRowsCount, this.TableRows.Count - saveRowsCount);
                    }
                }
            }
        }

        private void ProcessHtmlTableAndUpdateTitle(HtmlDocHolder docHolder, IElement table, int maxRowsToProcess, int tableIndex)
        {
            var debugSaveRowCount = this.TableRows.Count;
            if (table.QuerySelectorAll("*").Where(m => m.LocalName == "table").ToList().Count > 0)
            {
                Logger.Debug($"ignore table {tableIndex} with subtables");
            }
            else if (table.TextContent.Length > 0 && !table.TextContent.Any(x => char.IsUpper(x)))
            {
                Logger.Debug($"ignore table {tableIndex} that has no uppercase char");
            }
            else if (table.TextContent.Length < 30)
            {
                Logger.Debug($"ignore table {tableIndex}, it is too short");
            }
            else
            {
                this.ProcessHtmlTable(docHolder, table, maxRowsToProcess);
            }

            if (this.TableRows.Count > debugSaveRowCount)
            {
                var tableText = table.TextContent.Length > 30 ? table.TextContent.Substring(0, 30).ReplaceEolnWithSpace() : table.TextContent.ReplaceEolnWithSpace();
                Logger.Debug($"add {this.TableRows.Count - debugSaveRowCount} rows (TableRows.Count={this.TableRows.Count} ) from table {tableIndex} Table.innertText[0:30]='{tableText}'");
            }

            if (this.Title.Length == 0 && table.TextContent.Length > 30 && table.TextContent.IndexOf("декабря", StringComparison.OrdinalIgnoreCase) != -1)
            {
                var rows = new List<string>();
                foreach (var r in this.GetHtmlTableRows(table))
                {
                    rows.Add(r.TextContent);
                }

                this.Title = string.Join("\n", rows);
            }
        }

        private void CollectRows(HtmlDocHolder docHolder, int maxRowsToProcess)
        {
            var tables = docHolder.HtmlDocument.QuerySelectorAll("*").Where(m => m.LocalName == "table").ToList();
            var tableIndex = 0;
            this.TablesCount = tables.Count;
            foreach (var t in tables)
            {
                this.ProcessHtmlTableAndUpdateTitle(docHolder, t, maxRowsToProcess, tableIndex);
                tableIndex++;
            }

            this.TableRows = DropDayOfWeekRows(this.TableRows);
        }

        public override List<Cell> GetCells(int row, int maxColEnd = IAdapter.MaxColumnsCount)
        {
            var result = new List<Cell>();
            foreach (var r in this.TableRows[row])
            {
                result.Add(r);
            }

            return result;
        }

        public override Cell GetCell(int row, int column)
        {
            var cellNo = FindMergedCellByColumnNo(this.TableRows, row, column);
            return cellNo == -1 ? null : (Cell)this.TableRows[row][cellNo];
        }

        public override int GetRowsCount() => this.TableRows.Count;

        public override int GetColsCount() => this.UnmergedColumnsCount;

        public override int GetTablesCount() => this.TablesCount;
    }
}
