using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TI.Declarator.ParserCommon;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml.Linq;
using Smart.Parser.Lib.Adapters.AdapterSchemes;
using Smart.Parser.Lib.Adapters.DocxSchemes;

namespace Smart.Parser.Adapters
{
    public class TableWidthInfo
    {
        public int TableWidthInPixels;
        public int TableIndentionInPixels = 0;
        public List<int> ColumnWidths;

        public static int DxaToPixels(int dxa)
        {
            var points = dxa / 20.0;
            return (int)(((double)points) * 96.0 / 72.0);
        }

        public static int TryReadWidth(string val, TableWidthUnitValues widthType, int parentWidthInPixels)
        {
            try
            {
                if (widthType == TableWidthUnitValues.Pct)
                {
                    double pct = int.Parse(val);
                    var ratio = pct / 5000.0;
                    var pxels = parentWidthInPixels * ratio;
                    return (int)pxels;
                }

                if (widthType != TableWidthUnitValues.Dxa && widthType != TableWidthUnitValues.Auto)
                {
                    Console.WriteLine("unknown TableWidthUnitValues");
                    return 0;
                }

                return DxaToPixels(int.Parse(val));
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }

    public class WordDocHolder : IDisposable
    {
        public WordprocessingDocument WordDocument;
        public int DocumentPageSizeInPixels;
        public int DocumentPageLeftMaginInPixels = 0;
        public int DefaultFontSize = 10;
        public string DefaultFontName = "Times New Roman";
        private bool disposed = false;

        public WordDocHolder(WordprocessingDocument wordDocument)
        {
            this.WordDocument = wordDocument;
            this.InitPageSize();
            this.InitDefaultFontInfo();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.WordDocument.Dispose();
                // Free any other managed objects here.
                //
            }

            this.disposed = true;
        }

        private void InitPageSize()
        {
            var docPart = this.WordDocument.MainDocumentPart;
            var pageSize = docPart.Document.Descendants<PageSize>().FirstOrDefault();
            var pageDxa = 11906; // letter size is ISO 216 A4 (210x297mm
            if (pageSize != null)
            {
                pageDxa = (int)(uint)pageSize.Width;
            }

            this.DocumentPageSizeInPixels = TableWidthInfo.DxaToPixels(pageDxa);

            var pageMargin = docPart.Document.Descendants<PageMargin>().FirstOrDefault();
            var pageMarginDxa = 0; // letter size is ISO 216 A4 (210x297mm
            if (pageMargin?.Left != null)
            {
                pageMarginDxa = (int)(uint)pageMargin.Left;
            }

            this.DocumentPageLeftMaginInPixels = TableWidthInfo.DxaToPixels(pageMarginDxa);
        }

        private void InitDefaultFontInfo()
        {
            if (this.WordDocument.MainDocumentPart.StyleDefinitionsPart != null)
            {
                var defaults = this.WordDocument.MainDocumentPart.StyleDefinitionsPart.Styles.Descendants<DocDefaults>().FirstOrDefault();
                if (defaults.RunPropertiesDefault.RunPropertiesBaseStyle.FontSize != null)
                {
                    this.DefaultFontSize = int.Parse(defaults.RunPropertiesDefault.RunPropertiesBaseStyle.FontSize.Val);
                    if (defaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts.HighAnsi != null)
                    {
                        this.DefaultFontName = defaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts.HighAnsi;
                    }
                }
            }

            const string wordmlNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XNamespace w = wordmlNamespace;
            StylesPart stylesPart = this.WordDocument.MainDocumentPart.StyleDefinitionsPart;
            if (stylesPart != null)
            {
                XDocument styleDoc = null;
                using var reader = XmlNodeReader.Create(
                  stylesPart.GetStream(FileMode.Open, FileAccess.Read));
                // Create the XDocument.
                styleDoc = XDocument.Load(reader);
                foreach (var style in styleDoc.Descendants(w + "style"))
                {
                    var s = new Style(style.ToString());
                    if (s.Default == "1" && s.StyleRunProperties != null)
                    {
                        if (s.StyleRunProperties.FontSize != null)
                        {
                            this.DefaultFontSize = int.Parse(s.StyleRunProperties.FontSize.Val);
                        }

                        if (s.StyleRunProperties.RunFonts != null)
                        {
                            this.DefaultFontName = s.StyleRunProperties.RunFonts.HighAnsi;
                        }

                        break;
                    }
                }
            }
        }

        public string FindTitleAboveTheTable()
        {
            var title = string.Empty;
            var body = this.WordDocument.MainDocumentPart.Document.Body;
            foreach (var p in this.WordDocument.MainDocumentPart.Document.Descendants<Paragraph>())
            {
                if (p.Parent != body)
                {
                    break;
                }

                title += p.InnerText + "\n";
            }

            return title;
        }
    }

    public class OpenXmlWordCell : Cell
    {
        public bool IsVerticallyMerged;

        public OpenXmlWordCell(WordDocHolder docHolder, TableWidthInfo tableWidth, TableCell inputCell, int row, int column)
        {
            this.InitTextProperties(docHolder, inputCell);
            var vmerge = inputCell.TableCellProperties.GetFirstChild<VerticalMerge>();
            if (vmerge == null)
            {
                this.IsVerticallyMerged = false;
            }
            else
            {
                if (vmerge == null || vmerge.Val == null || vmerge.Val == MergedCellValues.Continue)
                {
                    this.IsVerticallyMerged = true;
                }
                else
                {
                    // vmerge.Val == MergedCellValues.Restart
                    this.IsVerticallyMerged = false;
                }
            }

            var gridSpan = inputCell.TableCellProperties.GetFirstChild<GridSpan>();
            this.IsMerged = gridSpan?.Val > 1;
            this.FirstMergedRow = -1; // init afterwards
            this.MergedRowsCount = -1; // init afterwards

            this.MergedColsCount = (gridSpan == null) ? 1 : (int)gridSpan.Val;
            this.Row = row;
            this.Col = column;
            if (inputCell
                .TableCellProperties?
                .TableCellWidth?
                .Type != null
                && inputCell.TableCellProperties.TableCellWidth.Type != TableWidthUnitValues.Auto)
            {
                this.CellWidth = TableWidthInfo.TryReadWidth(
                    inputCell.TableCellProperties.TableCellWidth.Width,
                    inputCell.TableCellProperties.TableCellWidth.Type,
                    tableWidth.TableWidthInPixels);
            }
            else
            {
                if (this.Col < tableWidth.ColumnWidths.Count)
                {
                    this.CellWidth = tableWidth.ColumnWidths[this.Col];
                }
            }

            this.AdditTableIndention = tableWidth.TableIndentionInPixels;
        }

        public OpenXmlWordCell(IAdapter.TJsonCell cell)
        {
            this.Text = cell.t;
            this.MergedColsCount = cell.mc;
            this.MergedRowsCount = cell.mr;
            this.IsVerticallyMerged = this.MergedRowsCount > 1;
            this.IsEmpty = this.Text.IsNullOrWhiteSpace();
            this.Row = cell.r;
            this.Col = cell.c;
        }

        private static int AfterLinesCount(SpacingBetweenLines pSpc)
        {
            if (pSpc == null)
            {
                return 0;
            }

            if (pSpc.AfterLines?.HasValue == true)
            {
                return pSpc.AfterLines;
            }
            else if (pSpc.After?.HasValue == true && pSpc.Line?.HasValue == true)
            {
                var linesApprox = double.Parse(pSpc.After.Value) / double.Parse(pSpc.Line.Value);
                return (int)Math.Round(linesApprox);
            }
            else
            {
                return 0;
            }
        }

        private void InitTextProperties(WordDocHolder docHolder, OpenXmlElement inputCell)
        {
            var s = string.Empty;
            this.FontName = string.Empty;
            this.FontSize = 0;
            foreach (var p in inputCell.Elements<Paragraph>())
            {
                foreach (var textOrBreak in p.Descendants())
                {
                    if (textOrBreak.LocalName == "r" && textOrBreak is Run)
                    {
                        var r = textOrBreak as Run;
                        var rProps = r.RunProperties;
                        if (rProps != null)
                        {
                            if (rProps.FontSize != null)
                            {
                                var runFontSize = int.Parse(rProps.FontSize.Val);
                                if (runFontSize <= 28)
                                {
                                    this.FontSize = runFontSize; // if font is too large, it is is an ocr error, ignore it
                                }
                            }

                            if (rProps.RunFonts != null)
                            {
                                this.FontName = rProps.RunFonts.ComplexScript;
                            }
                        }
                    }
                    else if (textOrBreak.LocalName == "t")
                    {
                        s += textOrBreak.InnerText;
                    }
                    else if (textOrBreak.LocalName == "cr")
                    {
                        s += "\n";
                    }
                    else if (textOrBreak.LocalName == "br")
                    /* do  not use lastRenderedPageBreak, see MinRes2011 for wrong lastRenderedPageBreak in Семенов
                    ||
                          (textOrBreak.Name == w + "lastRenderedPageBreak") */
                    {
                        s += "\n";
                    }
                    else if (textOrBreak.LocalName == "numPr")
                    {
                        s += "- ";
                    }
                }

                s += "\n";
                var pPr = p.ParagraphProperties;
                if (pPr != null)
                {
                    for (var l = 0; l < AfterLinesCount(pPr.SpacingBetweenLines); ++l)
                    {
                        s += "\n";
                    }
                }
            }

            this.Text = s;
            this.IsEmpty = s.IsNullOrWhiteSpace();
            if (string.IsNullOrEmpty(this.FontName))
            {
                this.FontName = docHolder.DefaultFontName;
            }

            if (this.FontSize == 0)
            {
                this.FontSize = docHolder.DefaultFontSize;
            }
        }
    }

    public class OpenXmlWordAdapter : IAdapter
    {
        private List<List<OpenXmlWordCell>> TableRows;
        private string Title;
        private int UnmergedColumnsCount;
        private const string WordXNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private readonly XmlNamespaceManager NamespaceManager;
        private int TablesCount;
        private readonly DocxConverter _DocxConverter;

        protected static List<IAdapterScheme> _allSchemes = new List<IAdapterScheme>()
        {
            new SovetFederaciiDocxScheme(),
            // new DocxSchemePDF(),
        };

        private static Uri FixUri(string brokenUri) => new Uri("http://broken-link/");

        private void ProcessDoc(string fileName, string extension, int maxRowsToProcess)
        {
            using var doc = new WordDocHolder(WordprocessingDocument.Open(fileName, false));
            this.CurrentScheme = _allSchemes.Find(x => x.CanProcess(doc.WordDocument));
            if (this.CurrentScheme != default)
            {
                // CollectRows from distinct Tables
                this.Title = doc.FindTitleAboveTheTable();
                this.CurrentScheme.Document = doc.WordDocument.MainDocumentPart.Document;
                this.TablesCount = 1;
            }
            else
            {
                this.Title = doc.FindTitleAboveTheTable();
                this.CollectRows(doc, maxRowsToProcess, extension);
                this.UnmergedColumnsCount = this.GetUnmergedColumnsCountByFirstRow();
                this.InitializeVerticallyMerge();
            }
        }

        public OpenXmlWordAdapter(string fileName, int maxRowsToProcess)
        {
            this._DocxConverter = new DocxConverter(ConvertedFileStorageUrl);
            this.NamespaceManager = new XmlNamespaceManager(new NameTable());
            this.NamespaceManager.AddNamespace("w", WordXNamespace);

            this.TableRows = new List<List<OpenXmlWordCell>>();

            if (fileName.EndsWith(".toloka_json"))
            {
                this.InitFromJson(fileName);
                this.UnmergedColumnsCount = this.GetUnmergedColumnsCountByFirstRow();
                return;
            }

            this.DocumentFile = fileName;
            var extension = Path.GetExtension(fileName).ToLower();
            var removeTempFile = false;
            if (extension == ".html"
                || extension == ".htm"
                || extension == ".xhtml"
                || extension == ".pdf"
                || extension == ".doc"
                || extension == ".rtf")
            {
                try
                {
                    fileName = this._DocxConverter.ConvertFile2TempDocX(fileName);
                }
                catch (TypeInitializationException exp)
                {
                    Logger.Error("Type Exception " + exp.ToString());
                    fileName = this._DocxConverter.ConvertWithSoffice(fileName);
                }
                catch (Exception exp)
                {
                    Logger.Error($"cannot convert {fileName} to docx, try one more time (exception: {exp}");
                    Thread.Sleep(10000); // 10 seconds
                    fileName = this._DocxConverter.ConvertFile2TempDocX(fileName);
                }

                removeTempFile = true;
            }

            try
            {
                this.ProcessDoc(fileName, extension, maxRowsToProcess);
            }
            catch (OpenXmlPackageException e)
            {
                // http://www.ericwhite.com/blog/handling-invalid-hyperlinks-openxmlpackageexception-in-the-open-xml-sdk/
                if (e.ToString().Contains("Invalid Hyperlink"))
                {
                    var newFileName = fileName + ".fixed.docx";
                    File.Copy(fileName, newFileName);
                    using (var fs = new FileStream(newFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        UriFixer.FixInvalidUri(fs, brokenUri => FixUri(brokenUri));
                    }

                    this.ProcessDoc(newFileName, extension, maxRowsToProcess);
                    File.Delete(newFileName);
                }
            }

            if (removeTempFile)
            {
                File.Delete(fileName);
            }
        }

        public static IAdapter CreateAdapter(string fileName, int maxRowsToProcess) =>
            // if (OnePersonAdapter.CanProcess(fileName))
            //     // throw new SmartParserException("Impossible to parse one-person file");
            //     return new OnePersonAdapter(fileName);

            new OpenXmlWordAdapter(fileName, maxRowsToProcess);

        private void CopyPortion(List<List<TJsonCell>> portion, bool ignoreMergedRows)
        {
            for (var i = 0; i < portion.Count; i++)
            {
                var r = portion[i];
                var newRow = new List<OpenXmlWordCell>();

                foreach (var c in r)
                {
                    var cell = new OpenXmlWordCell(c)
                    {
                        Row = this.TableRows.Count,
                    };
                    if (ignoreMergedRows)
                    {
                        cell.MergedRowsCount = 1;
                    }

                    cell.CellWidth = 10; // no cell width serialized in html
                    newRow.Add(cell);
                }

                this.TableRows.Add(newRow);
            }
        }

        private void InitFromJson(string fileName)
        {
            string jsonStr;
            using (var r = new StreamReader(fileName))
            {
                jsonStr = r.ReadToEnd();
            }

            var portion = JsonConvert.DeserializeObject<TJsonTablePortion>(jsonStr);
            this.Title = portion.Title;
            this.DocumentFile = portion.InputFileName;
            this.CopyPortion(portion.Header, false);
            this.CopyPortion(portion.Section, true);
            this.CopyPortion(portion.Data, true);
        }

        public override string GetTitleOutsideTheTable() => this.Title;

        private int FindFirstBorderGoingUp(int startRow, int column)
        {
            for (var i = startRow; i > 0; --i)
            {
                var cellNo = FindMergedCellByColumnNo(this.TableRows, i, column);
                if (cellNo == -1)
                {
                    return i + 1;
                }

                if (!this.TableRows[i][cellNo].IsVerticallyMerged)
                {
                    return i;
                }

                if (i == 0)
                {
                    return i;
                }
            }

            return 0;
        }

        private int FindFirstBorderGoingDown(int startRow, int column)
        {
            for (var i = startRow; i < this.TableRows.Count; ++i)
            {
                var cellNo = FindMergedCellByColumnNo(this.TableRows, i, column);
                if (cellNo == -1)
                {
                    return i - 1;
                }

                if (i > startRow && !this.TableRows[i][cellNo].IsVerticallyMerged)
                {
                    return i - 1;
                }

                if (i + 1 == this.TableRows.Count)
                {
                    return i;
                }
            }

            return this.TableRows.Count - 1;
        }

        private void InitializeVerticallyMerge()
        {
            foreach (var r in this.TableRows)
            {
                foreach (var c in r)
                {
                    try
                    {
                        c.FirstMergedRow = this.FindFirstBorderGoingUp(c.Row, c.Col);
                        c.MergedRowsCount = this.FindFirstBorderGoingDown(c.Row, c.Col) - c.Row + 1;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Parsing Exception row{c.Row} col={c.Col}: {e}");
                        throw;
                    }
                }
            }
        }

        private int GetRowGridBefore(TableRow row)
        {
            if (row.TableRowProperties != null)
            {
                foreach (var c in row.TableRowProperties.Descendants<GridBefore>())
                {
                    return c.Val;
                }
            }

            return 0;
        }

        private TableWidthInfo InitializeTableWidthInfo(WordDocHolder docHolder, Table table)
        {
            var widthInfo = new TableWidthInfo();
            var tProp = table.GetFirstChild<TableProperties>();
            if (tProp != null)
            {
                if (tProp.TableWidth != null)
                {
                    widthInfo.TableWidthInPixels = TableWidthInfo.TryReadWidth(
                        tProp.TableWidth.Width,
                        tProp.TableWidth.Type,
                        docHolder.DocumentPageSizeInPixels);
                }

                if (tProp.TableIndentation != null)
                {
                    widthInfo.TableIndentionInPixels = TableWidthInfo.TryReadWidth(
                        tProp.TableIndentation.Width,
                        tProp.TableIndentation.Type,
                        docHolder.DocumentPageSizeInPixels);
                }

                widthInfo.TableIndentionInPixels += docHolder.DocumentPageLeftMaginInPixels;
            }
            else
            {
                widthInfo.TableWidthInPixels = docHolder.DocumentPageSizeInPixels;
            }

            var tGrid = table.GetFirstChild<TableGrid>();
            if (tGrid != null)
            {
                widthInfo.ColumnWidths = new List<int>();
                foreach (var col in tGrid.Elements<GridColumn>())
                {
                    widthInfo.ColumnWidths.Add(
                        TableWidthInfo.TryReadWidth(
                            col.Width,
                            TableWidthUnitValues.Dxa,
                            widthInfo.TableWidthInPixels));
                }
            }

            return widthInfo;
        }

        private void ProcessWordTable(WordDocHolder docHolder, Table table, int maxRowsToProcess)
        {
            var rows = table.Descendants<TableRow>().ToList();
            var widthInfo = this.InitializeTableWidthInfo(docHolder, table);
            var saveRowsCount = this.TableRows.Count;
            var maxCellsCount = 0;
            for (var r = 0; r < rows.Count; ++r)
            {
                var newRow = new List<OpenXmlWordCell>();
                var sumspan = 0;
                var row = rows[r];
                var rowGridBefore = this.GetRowGridBefore(row);
                var isEmpty = true;
                foreach (var rowCell in row.Elements<TableCell>())
                {
                    var c = new OpenXmlWordCell(docHolder, widthInfo, rowCell, this.TableRows.Count, sumspan);
                    if (newRow.Count == 0)
                    {
                        c.MergedColsCount += rowGridBefore;
                    }

                    newRow.Add(c);
                    sumspan += c.MergedColsCount;
                    isEmpty = isEmpty && c.IsEmpty;
                }

                if (isEmpty)
                {
                    continue;
                }

                maxCellsCount = Math.Max(newRow.Count, maxCellsCount);
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

            if (maxCellsCount <= 4 || CheckNameColumnIsEmpty(this.TableRows, saveRowsCount))
            {
                // remove this suspicious table
                this.TableRows.RemoveRange(saveRowsCount, this.TableRows.Count - saveRowsCount);
            }
        }

        private void ProcessWordTableAndUpdateTitle(WordDocHolder docHolder, Table table, int maxRowsToProcess, int tableIndex)
        {
            var debugSaveRowCount = this.TableRows.Count;
            if (table.Descendants<Table>().ToList().Count > 0)
            {
                Logger.Debug($"ignore table {tableIndex} with subtables");
            }
            else if (table.InnerText.Length > 0 && !table.InnerText.Any(x => char.IsUpper(x)))
            {
                Logger.Debug($"ignore table {tableIndex} that has no uppercase char");
            }
            else if (table.InnerText.Length < 30)
            {
                Logger.Debug($"ignore table {tableIndex}, it is too short");
            }
            else
            {
                this.ProcessWordTable(docHolder, table, maxRowsToProcess);
            }

            if (this.TableRows.Count > debugSaveRowCount)
            {
                var tableText = table.InnerText.Length > 30 ? table.InnerText.Substring(0, 30) : table.InnerText;
                Logger.Debug($"add {this.TableRows.Count - debugSaveRowCount} rows (TableRows.Count={this.TableRows.Count} ) from table {tableIndex} Table.innertText[0:30]='{tableText}'");
            }

            if (this.Title.Length == 0 && table.InnerText.Length > 30 && table.InnerText.IndexOf("декабря", StringComparison.OrdinalIgnoreCase) != -1)
            {
                var rows = new List<string>();
                foreach (var r in table.Descendants<TableRow>())
                {
                    rows.Add(r.InnerText);
                }

                this.Title = string.Join("\n", rows);
            }
        }

        private void CollectRows(WordDocHolder docHolder, int maxRowsToProcess, string extension)
        {
            var docPart = docHolder.WordDocument.MainDocumentPart;
            var tables = docPart.Document.Descendants<Table>().ToList();
            var tableIndex = 0;
            foreach (OpenXmlPart h in docPart.HeaderParts)
            {
                foreach (var t in h.RootElement.Descendants<Table>())
                {
                    this.ProcessWordTableAndUpdateTitle(docHolder, t, maxRowsToProcess, tableIndex);
                    tableIndex++;
                }
            }

            if (extension != ".htm" && extension != ".html") // это просто костыль. Нужно как-то встроить это в архитектуру.
            {
                tables = ExtractSubtables(tables);
            }

            this.TablesCount = tables.Count;
            foreach (var t in tables)
            {
                this.ProcessWordTableAndUpdateTitle(docHolder, t, maxRowsToProcess, tableIndex);
                tableIndex++;
            }

            this.TableRows = DropDayOfWeekRows(this.TableRows);
        }

        private static List<Table> ExtractSubtables(List<Table> tables)
        {
            var tablesWithDescendants = tables.Where(x => x.Descendants<Table>().Any());

            foreach (var t in tablesWithDescendants)
            {
                var extractedTables = t.Descendants<Table>().ToList();
                extractedTables = ExtractSubtables(extractedTables);
                tables = tables.Concat(extractedTables).ToList();
                foreach (var td in t.Descendants<Table>())
                {
                    td.Remove();
                }
            }

            return tables;
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