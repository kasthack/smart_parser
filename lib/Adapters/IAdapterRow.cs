using Parser.Lib;
using System.Collections.Generic;
using System.Linq;
using Smart.Parser.Lib;
using TI.Declarator.ParserCommon;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Text;

namespace Smart.Parser.Adapters
{
    public class Cell
    {
        public virtual bool IsMerged { get; set; } = false;

        public virtual int FirstMergedRow { get; set; } = -1;

        public virtual int MergedRowsCount { get; set; } = -1;

        public virtual int MergedColsCount { get; set; } = 1;

        public virtual bool IsEmpty { get; set; } = true;

        public virtual string Text { get; set; } = string.Empty;

        public string TextAbove = null;

        public Cell ShallowCopy() => (Cell)this.MemberwiseClone();

        public virtual string GetText(bool trim = true)
        {
            var text = this.Text;
            if (trim)
            {
                text = text.CoalesceWhitespace().Trim();
            }

            return text;
        }

        public override string ToString() => this.Text;

        public List<string> GetLinesWithSoftBreaks()
        {
            var res = new List<string>();
            if (this.IsEmpty)
            {
                return res;
            }

            var hardLines = this.Text.Split('\n');
            var graphics = System.Drawing.Graphics.FromImage(new Bitmap(1, 1));
            graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;

            var stringSize = new SizeF();
            var font = new Font(this.FontName, this.FontSize / 2);
            foreach (var hardLine in hardLines)
            {
                stringSize = graphics.MeasureString(hardLine, font);
                // Logger.Info("stringSize = {0} (FontName = {2}, fontsize = {1})", stringSize, FontSize / 2, FontName);

                const int defaultMargin = 11; // to do calc it really
                var softLinesCount = (int)(stringSize.Width / (this.CellWidth - defaultMargin)) + 1;
                if (softLinesCount == 1)
                {
                    res.Add(hardLine);
                }
                else
                {
                    var start = 0;
                    for (var k = 0; k < softLinesCount; ++k)
                    {
                        int len;
                        if (k + 1 == softLinesCount)
                        {
                            len = hardLine.Length - start;
                        }
                        else
                        {
                            len = hardLine.Length / softLinesCount;
                            var wordBreak = (start + len >= hardLine.Length) ? hardLine.Length : hardLine.LastIndexOf(' ', start + len);
                            if (wordBreak > start)
                            {
                                len = wordBreak - start;
                            }
                            else
                            {
                                wordBreak = hardLine.IndexOf(' ', start + 1);
                                len = (wordBreak == -1) ? hardLine.Length - start : wordBreak - start;
                            }
                        }

                        res.Add(hardLine.Substring(start, len));
                        start += len;
                        if (start >= hardLine.Length)
                        {
                            break;
                        }
                    }
                }
            }

            // Logger.Info("result = {0}", string.Join("|\n", res));
            return res;
        }

        public int Row { get; set; } = -1;

        public int Col { get; set; } = -1; // not merged column index

        public int CellWidth = 0; // in pixels
        public int AdditTableIndention = 0; // only for Word: http://officeopenxml.com/WPtableIndent.php
        public string FontName;
        public int FontSize;
    }

    public class DataRow : DataRowInterface
    {
        private void MapCells() => this.MappedHeader = MapByOrderAndIntersection(this.ColumnOrdering, this.Cells) ?? MapByMaxIntersection(this.ColumnOrdering, this.Cells);

        public DataRow(IAdapter adapter, ColumnOrdering columnOrdering, int row)
        {
            this.row = row;
            this.adapter = adapter;
            this.ColumnOrdering = columnOrdering;
            this.Cells = adapter.GetCells(row, columnOrdering.GetMaxColumnEndIndex());
            if (!this.adapter.IsExcel())
            {
                this.MapCells();
            }
        }

        public string DebugString()
        {
            var s = string.Empty;
            foreach (var c in this.Cells)
            {
                s += $"\"{c.Text.Replace("\n", "\\n")}\"[{c.CellWidth}], ";
            }

            return s;
        }

        public DataRow DeepClone()
        {
            var other = new DataRow(this.adapter, this.ColumnOrdering, this.row)
            {
                Cells = new List<Cell>(),
            };
            foreach (var x in this.Cells)
            {
                var c = x.ShallowCopy();
                c.IsEmpty = true;
                c.Text = string.Empty;
                other.Cells.Add(c);
            }

            other.MapCells();
            return other;
        }

        private static Dictionary<DeclarationField, Cell> MapByOrderAndIntersection(ColumnOrdering columnOrdering, List<Cell> cells)
        {
            if (columnOrdering.MergedColumnOrder.Count != cells.Count)
            {
                return null;
            }

            var start = cells[0].AdditTableIndention;
            var res = new Dictionary<DeclarationField, Cell>();
            var pixelErrorCount = 0;
            for (var i = 0; i < cells.Count; i++)
            {
                var s1 = start;
                var e1 = start + cells[i].CellWidth;
                var colInfo = columnOrdering.MergedColumnOrder[i];
                var s2 = colInfo.ColumnPixelStart;
                var e2 = colInfo.ColumnPixelStart + colInfo.ColumnPixelWidth;
                if (ColumnOrdering.PeriodIntersection(s1, e1, s2, e2) == 0)
                {
                    pixelErrorCount++;
                    if (!DataHelper.IsEmptyValue(cells[i].Text))
                    {
                        if (!ColumnPredictor.TestFieldWithoutOwntypes(colInfo.Field, cells[i]))
                        {
                            Logger.Debug($"cannot map column N={i} text={cells[i].Text.Replace("\n", "\\n")}");
                            return null;
                        }
                        else
                        {
                            Logger.Debug($"found semantic argument for mapping N={i} text={cells[i].Text.Replace("\n", "\\n")} to {colInfo.Field}");
                            pixelErrorCount = 0;
                        }
                    }
                }

                res[columnOrdering.MergedColumnOrder[i].Field] = cells[i];

                start = e1;
            }

            return pixelErrorCount >= 3 ? null : res;
        }

        private static Dictionary<DeclarationField, Cell> MapByMaxIntersection(ColumnOrdering columnOrdering, List<Cell> cells)
        {
            Logger.Debug("MapByMaxIntersection");
            // map two header cells to one data cell
            // see dnko-2014.docx for an example

            var res = new Dictionary<DeclarationField, Cell>();
            var sizes = new Dictionary<DeclarationField, int>();
            if (cells.Count == 0)
            {
                return res;
            }

            var start = cells[0].AdditTableIndention;
            foreach (var c in cells)
            {
                if (c.CellWidth > 0)
                {
                    var field = columnOrdering.FindByPixelIntersection(start, start + c.CellWidth, out var interSize);

                    // cannot map some text,so it is a failure
                    if (field == DeclarationField.None && c.Text.Trim().Length > 0)
                    {
                        return null;
                    }

                    // take only fields with maximal pixel intersection
                    if (!sizes.ContainsKey(field) || sizes[field] < interSize)
                    {
                        // Logger.Debug(string.Format("map {1} to {0}", field, c.Text.Replace("\n", "\\n")));
                        res[field] = c;
                        sizes[field] = interSize;
                    }
                }

                start += c.CellWidth;
            }

            return res;
        }

        public bool IsEmpty(params DeclarationField[] fields) => fields.All(field => this.GetContents(field, false).IsNullOrWhiteSpace());

        public int GetRowIndex() => this.Cells[0].Row;

        public void Merge(DataRow other)
        {
            for (var i = 0; i < this.Cells.Count && i < other.Cells.Count; i++)
            {
                this.Cells[i].Text += " " + other.Cells[i].Text;
            }
        }

        public Cell GetDeclarationField(DeclarationField field)
        {
            if (this.MappedHeader != null && this.MappedHeader.TryGetValue(field, out var cell))
            {
                return cell;
            }

            var exactCell = this.adapter.GetDeclarationFieldWeak(this.ColumnOrdering, this.row, field, out var colSpan);
            if (exactCell.Text.Trim() != string.Empty || exactCell.Col == -1)
            {
                return exactCell;
            }

            for (var i = exactCell.Col + exactCell.MergedColsCount; i < colSpan.EndColumn;)
            {
                var mergedCell = this.adapter.GetCell(this.row, i);
                if (mergedCell == null)
                {
                    break;
                }

                if (mergedCell.Text.Trim() != string.Empty)
                {
                    return mergedCell;
                }

                i += mergedCell.MergedColsCount;
            }

            return exactCell;
        }

        public string GetContents(DeclarationField field, bool except = true)
        {
            if (!this.ColumnOrdering.ContainsField(field) && !except)
            {
                return string.Empty;
            }

            Cell c;
            try
            {
                c = this.GetDeclarationField(field);
            }
            catch (SmartParserFieldNotFoundException)
            {
                if (!except)
                {
                    return string.Empty;
                }

                throw;
            }

            return c == null ? string.Empty : c.GetText(true);
        }

        public bool IsEmpty() => this.Cells.All(cell => cell.Text.IsNullOrWhiteSpace());

        public int? GetPersonIndex()
        {
            int? index = null;
            if (this.ColumnOrdering.ContainsField(DeclarationField.Number))
            {
                var indexStr = this.GetDeclarationField(DeclarationField.Number).Text
                    .Replace(".", string.Empty).ReplaceEolnWithSpace();
                var dummyRes = int.TryParse(indexStr, out var indVal);
                if (dummyRes)
                {
                    index = indVal;
                }
            }

            return index;
        }

        public void SetRelative(string value)
        {
            if (DataHelper.IsEmptyValue(value))
            {
                value = string.Empty;
            }

            this.RelativeType = value;
            if (this.RelativeType != string.Empty && !DataHelper.IsRelativeInfo(this.RelativeType))
            {
                throw new SmartParserException($"Wrong relative type {this.RelativeType} at row {this.GetRowIndex()}");
            }
        }

        private void DivideNameAndOccupation()
        {
            var nameCell = this.GetDeclarationField(DeclarationField.NameAndOccupationOrRelativeType);
            this.NameDocPosition = this.adapter.GetDocumentPosition(this.GetRowIndex(), nameCell.Col);

            var v = nameCell.GetText(true);
            if (DataHelper.IsEmptyValue(v))
            {
                return;
            }

            if (DataHelper.IsRelativeInfo(v))
            {
                this.SetRelative(v);
            }
            else
            {
                const string pattern = @"\s+\p{Pd}\s+"; // UnicodeCategory.DashPunctuation
                v = Regex.Replace(v, @"\d+\.\s+", string.Empty);
                var two_parts = Regex.Split(v, pattern);
                var clean_v = Regex.Replace(v, pattern, " ");
                var words = Regex.Split(clean_v, @"[\,\s\n]+");

                if (words.Length >= 3 && TextHelpers.CanBePatronymic(words[2])
                                      && !TextHelpers.MayContainsRole(words[0])
                                      && !TextHelpers.MayContainsRole(words[1]))
                {
                    // ex: "Рутенберг Дмитрий Анатольевич начальник управления"
                    this.PersonName = string.Join(" ", words.Take(3)).Trim();
                    this.Occupation = string.Join(" ", words.Skip(3)).Trim();
                }
                else if (TextHelpers.CanBePatronymic(words.Last()))
                {
                    // ex: "начальник управления Рутенберг Дмитрий Анатольевич"
                    this.PersonName = string.Join(" ", words.Skip(words.Length - 3)).Trim();
                    this.Occupation = string.Join(" ", words.Take(words.Length - 3)).Trim();
                }
                else if (words.Length >= 2 && TextHelpers.CanBeInitials(words[1]) && TextHelpers.MayContainsRole(string.Join(" ", words.Skip(2)).Trim()))
                {
                    // ex: "Головачева Н.В., заместитель"
                    this.PersonName = string.Join(" ", words.Take(2)).Trim();
                    this.Occupation = string.Join(" ", words.Skip(2)).Trim();
                }
                else if (two_parts.Length == 2)
                {
                    this.PersonName = two_parts[0].Trim();
                    this.Occupation = string.Join(" - ", two_parts.Skip(1)).Trim();
                }
                else
                {
                    throw new SmartParserException(
                        $"Cannot parse name+occupation value {v} at row {this.GetRowIndex()}");
                }
            }
        }

        public bool InitPersonData(string prevPersonName)
        {
            if (this.ColumnOrdering.ContainsField(DeclarationField.RelativeTypeStrict))
            {
                this.SetRelative(this.GetDeclarationField(DeclarationField.RelativeTypeStrict).Text.ReplaceEolnWithSpace());
            }

            string nameOrRelativeType;
            if (this.ColumnOrdering.ContainsField(DeclarationField.NameAndOccupationOrRelativeType))
            {
                if (!ColumnOrdering.SearchForFioColumnOnly)
                {
                    try
                    {
                        this.DivideNameAndOccupation();
                    }
                    catch (SmartParserException)
                    {
                        // maybe PDF has split cells (table on different pages)
                        // example file: "5966/14 Upravlenie delami.pdf" converted to docx
                        var nameCell = this.GetDeclarationField(DeclarationField.NameAndOccupationOrRelativeType);
                        Logger.Error("ignore bad person name " + nameCell);
                        return false;
                    }
                }
            }
            else
            {
                var nameCell = this.GetDeclarationField(DeclarationField.NameOrRelativeType);
                nameOrRelativeType = nameCell.Text.ReplaceEolnWithSpace().Replace("не имеет", string.Empty);
                this.NameDocPosition = this.adapter.GetDocumentPosition(this.GetRowIndex(), nameCell.Col);
                if (this.ColumnOrdering.ContainsField(DeclarationField.Occupation))
                {
                    this.Occupation = this.GetDeclarationField(DeclarationField.Occupation).Text;
                }

                if (this.ColumnOrdering.ContainsField(DeclarationField.Department))
                {
                    this.Department = this.GetDeclarationField(DeclarationField.Department).Text;
                }

                if (!DataHelper.IsEmptyValue(nameOrRelativeType))
                {
                    if (DataHelper.IsRelativeInfo(nameOrRelativeType))
                    {
                        this.SetRelative(nameOrRelativeType);
                    }
                    else if (prevPersonName == nameOrRelativeType && DataHelper.IsRelativeInfo(this.Occupation))
                    {
                        this.SetRelative(this.Occupation);
                    }
                    else
                    {
                        this.PersonName = nameOrRelativeType;
                        if (!this.PersonName.Contains('.') && !this.PersonName.Trim().Any(char.IsWhiteSpace))
                        {
                            Logger.Error("ignore bad person name " + this.PersonName);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public List<Cell> Cells;
        private readonly IAdapter adapter;
        public ColumnOrdering ColumnOrdering;
        private readonly int row;
        private Dictionary<DeclarationField, Cell> MappedHeader = null;

        // Initialized by InitPersonData
        public string PersonName = string.Empty;
        public string RelativeType = string.Empty;
        public string NameDocPosition = string.Empty;
        public string Occupation = string.Empty;
        public string Department = null;
    }
}
