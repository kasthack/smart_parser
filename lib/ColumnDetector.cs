using Parser.Lib;
using Smart.Parser.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TI.Declarator.ParserCommon;

namespace Smart.Parser.Lib
{
    public class ColumnDetectorException : Exception
    {
        public ColumnDetectorException(string message) : base(message)
        {
        }
    }

    public class ColumnDetector
    {
        public static List<string> AbsenceMarkers = new List<string> { "-", "отсутствует", "?", "не указано", "не имеет"};

        static public bool GetValuesFromTitle(string text, ref string title, ref int? year, ref string ministry)
        {
            var text_len = text.Length;
            if (title == null)
            {
                title = text;
            }
            else
            {
                title += " " + text;
            }

            text = text.ToLower();
            string[] title_words = { "сведения", "обязательствах", "доход", "период" };
            var has_title_words = Array.Exists(title_words, s => text.Contains(s));
            if (!has_title_words)
            {
                return false;
            }

            text = Regex.Replace(text, "8\\s+июля\\s+2013", "");

            var decemberYearMatches = Regex.Matches(text, @"(31\s+декабря\s+)(20\d\d)(\s+года)");
            if (decemberYearMatches.Count > 0)
            {
                year = int.Parse(decemberYearMatches[0].Groups[2].Value);
            }
            else
            {
                var commonYearMatches = Regex.Matches(text, @"\b20\d\d\b");
                if (commonYearMatches.Count > 0)
                {
                    year = int.Parse(commonYearMatches[0].Value);
                }
            }

            var specificYearMatches = Regex.Matches(text, @"за(20\d\d)\b");
            if (specificYearMatches.Count > 0)
            {
                year = int.Parse(specificYearMatches[0].Groups[1].Value);
            }

            var minMatch = Regex.Match(text, "Министерства(.+)Российской Федерации", RegexOptions.IgnoreCase);
            if (minMatch.Success)
            {
                ministry = minMatch.Groups[1].Value;
            }

            return true;
        }

        public static bool WeakHeaderCheck(List<Cell> cells)
        {
            var colCount = 0;
            if (cells.Count < 3)
            {
                return false;
            }

            foreach (var c in cells)
            {
                if (colCount == 0 && c.Text.IsNumber())
                {
                    return true;
                }

                if (c.Text.IsName())
                {
                    return true;
                }

                colCount++;
                if (colCount > 3)
                {
                    break;
                }
            }
            return false;
        }

        private static int ProcessTitle(IAdapter adapter, ColumnOrdering columnOrdering)
        {
            var row = 0;
            string title = null;
            string ministry = null;
            int? year = null;

            var findTitle = false;
            var prevRowIsSection = false;
            while (true)
            {
                var currRow = adapter.GetCells(row);
                var isSection = IAdapter.IsSectionRow(currRow, adapter.GetColsCount(), prevRowIsSection, out var section_text);
                if (isSection)
                {
                    if (section_text.Length > 20)
                    {
                        if (GetValuesFromTitle(section_text, ref title, ref year, ref ministry))
                        {
                            findTitle = true;
                        }
                    }
                    else
                    {
                        columnOrdering.Section = section_text;
                    }
                }
                else if (WeakHeaderCheck(currRow))
                {
                    break;
                }

                row++;

                if (row >= adapter.GetRowsCount())
                {
                    row = 0;
                    break;
                    throw new ColumnDetectorException(string.Format("Headers not found"));
                }
                prevRowIsSection = isSection;
            }
            if (!findTitle && GetValuesFromTitle(adapter.GetTitleOutsideTheTable(), ref title, ref year, ref ministry))
            {
                findTitle = true;
            }

            if (findTitle)
            {
                columnOrdering.Title = title;
                columnOrdering.Year = year;
                columnOrdering.MinistryName = ministry;
            }
            return row;
        }
        private static List<Cell> FindSubcellsUnder(IAdapter adapter, Cell cell)
        {
            var subCells = new List<Cell>();
            if (cell.Row + cell.MergedRowsCount >= adapter.GetRowsCount() )
            {
                return subCells;
            }
            if (cell.CellWidth ==  0 && cell.GetText(true).Trim()?.Length == 0)
            {
                return subCells;
            }
            var undercCells = adapter.GetCells(cell.Row + cell.MergedRowsCount);
            foreach (var underCell in undercCells)
            {
                if (underCell.Col < cell.Col)
                {
                    continue;
                }

                if (underCell.Col >= cell.Col + cell.MergedColsCount)
                {
                    break;
                }

                if (!underCell.IsEmpty)
                {
                    subCells.Add(underCell);
                }
            }
            return subCells;
        }
        private static void AddColumn(ColumnOrdering ordering, DeclarationField field, Cell  cell)
        {
            var s = new TColumnInfo
            {
                BeginColumn = cell.Col,
                EndColumn = cell.Col + cell.MergedColsCount,
                ColumnPixelWidth = cell.CellWidth,
                //s.ColumnPixelStart is unknown and initialized in FinishOrderingBuilding
                Field = field
            };
            if (field == DeclarationField.DeclaredYearlyIncome)
            {
                var dummy = "";
                ColumnDetector.GetValuesFromTitle(cell.GetText(), ref dummy, ref ordering.YearFromIncome, ref dummy);
            }

            ordering.Add(s);
        }

        private static void FixMissingSubheadersForVehicle(IAdapter adapter, ColumnOrdering columnOrdering)
        {
            if (!columnOrdering.ContainsField(DeclarationField.Vehicle))
            {
                return;
            }

            var headerCell = adapter.GetDeclarationFieldWeak(columnOrdering, columnOrdering.HeaderBegin.Value, DeclarationField.Vehicle, out var dummy);
            if (headerCell.MergedColsCount != 2)
            {
                return;
            }

            var subCells = FindSubcellsUnder(adapter, headerCell);
            if (subCells.Count == 1)
            {
                return;
            }

            var cleanHeader = headerCell.Text.ToLower().Replace(" ", "");
            if (cleanHeader.Contains("транспортныесредства") && cleanHeader.Contains("марка") && cleanHeader.Contains("вид"))
            {
                var columnVehicleType = new TColumnInfo
                {
                    BeginColumn = headerCell.Col,
                    EndColumn = headerCell.Col + 1,
                    ColumnPixelWidth = headerCell.CellWidth / 2,
                    Field = DeclarationField.VehicleType
                };
                columnOrdering.Add(columnVehicleType);

                var columnVehicleModel = new TColumnInfo
                {
                    BeginColumn = headerCell.Col + 1,
                    EndColumn = headerCell.Col + 2,
                    ColumnPixelWidth = headerCell.CellWidth / 2,
                    Field = DeclarationField.VehicleModel
                };
                columnOrdering.Add(columnVehicleModel);

                columnOrdering.Delete(DeclarationField.Vehicle);
            }
        }

        private static void FixMissingSubheadersForMixedRealEstate(IAdapter adapter, ColumnOrdering columnOrdering)
        {
            //see DepEnergo2010.doc  in tests
            if (!columnOrdering.ContainsField(DeclarationField.MixedColumnWithNaturalText))
            {
                return;
            }
            var headerCell = adapter.GetDeclarationFieldWeak(columnOrdering, columnOrdering.HeaderBegin.Value, DeclarationField.MixedColumnWithNaturalText, out var dummy);
            var subCells = FindSubcellsUnder(adapter, headerCell);
            if (subCells.Count != 3)
            {
                return;
            }
            for (var row = columnOrdering.FirstDataRow; row < adapter.GetRowsCount(); row++)
            {
                if (row > columnOrdering.FirstDataRow + 5)
                {
                    break;
                }
                // we check only the  second column, todo check the  first one and  the third
                var areaStr = adapter.GetCell(row, subCells[1].Col).GetText(true);
                if (!DataHelper.ParseSquare(areaStr).HasValue)
                {
                    return;
                }
            }
            AddColumn(columnOrdering, DeclarationField.MixedRealEstateType, subCells[0]);
            AddColumn(columnOrdering, DeclarationField.MixedRealEstateSquare, subCells[1]);
            AddColumn(columnOrdering, DeclarationField.MixedRealEstateCountry, subCells[2]);
            columnOrdering.Delete(DeclarationField.MixedColumnWithNaturalText);
        }

        private static void FixBadColumnName01_Template(ColumnOrdering c, DeclarationField naturalText, DeclarationField country, DeclarationField square, DeclarationField type)
        {
            //move MixedColumnWithNaturalText  to MixedRealEstateType
            if (!c.ContainsField(naturalText))
            {
                return;
            }

            if (c.ContainsField(country)
                    && c.ContainsField(square)
                )
            {
                var s = c.ColumnOrder[naturalText];
                s.Field = type;
                c.Add(s);
                c.Delete(naturalText);
            }
        }

        private static void FixBadColumnName01(ColumnOrdering c)
        {
            FixBadColumnName01_Template(c,
                DeclarationField.MixedColumnWithNaturalText,
                DeclarationField.MixedRealEstateCountry,
                DeclarationField.MixedRealEstateSquare,
                DeclarationField.MixedRealEstateType);
            FixBadColumnName01_Template(c,
                DeclarationField.StateColumnWithNaturalText,
                DeclarationField.StatePropertyCountry,
                DeclarationField.StatePropertySquare,
                DeclarationField.StatePropertyType);
            FixBadColumnName01_Template(c,
                DeclarationField.OwnedColumnWithNaturalText,
                DeclarationField.OwnedRealEstateCountry,
                DeclarationField.OwnedRealEstateSquare,
                DeclarationField.OwnedRealEstateType);
        }

        private static void FixBadColumnName02(ColumnOrdering c)
        {
            //move NameAndOccupationOrRelativeType  to NameOrRelativeType if Occupation  is present
            if (     c.ContainsField(DeclarationField.NameAndOccupationOrRelativeType)
                  && c.ContainsField(DeclarationField.Occupation)
                )
            {
                var s = c.ColumnOrder[DeclarationField.NameAndOccupationOrRelativeType];
                s.Field = DeclarationField.NameOrRelativeType;
                c.Add(s);
                c.Delete(DeclarationField.NameAndOccupationOrRelativeType);
            }
        }

        static public ColumnOrdering ExamineTableBeginning(IAdapter adapter)
        {
            var columnOrdering = new ColumnOrdering();
            var headerStartRow = ProcessTitle(adapter, columnOrdering);
            ReadHeader(adapter, headerStartRow, columnOrdering);
            return columnOrdering;
        }

        static public List<Cell> GetColumnCells(IAdapter adapter, int headerStartRow, out int headerEndRow)
        {
            headerEndRow = headerStartRow + 1;
            var firstRow = adapter.GetCells(headerStartRow);

            var columnCells =  new List<Cell>();
            var headerCanHaveSecondLevel = true;
            var maxMergedRows = 1;
            var texts = new List<string>();
            foreach (var cell in firstRow)
            {
                var text = cell.GetText(true);

                if (adapter.GetRowsCount() == cell.MergedRowsCount)
                {
                    continue;
                }

                if (cell.CellWidth == 0 && text.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (maxMergedRows < cell.MergedRowsCount)
                {
                    maxMergedRows = cell.MergedRowsCount;
                }

                var underCells = FindSubcellsUnder(adapter, cell);

                if (underCells.Count <= 1 || !headerCanHaveSecondLevel)
                {
                    headerEndRow = Math.Max(headerEndRow, cell.Row + cell.MergedRowsCount);

                    // иногда в двухярусном заголовке в верхней клетке пусто, а в нижней есть заголовок (TwoRowHeaderEmptyTopCellTest)
                    if (text.IsNullOrWhiteSpace() && cell.MergedRowsCount < maxMergedRows && underCells.Count == 1)
                    {
                        columnCells.Add(underCells.First());
                    }
                    else
                    {
                        columnCells.Add(cell);
                    }

                    texts.Add(cell.Text.NormSpaces());

                    // обработка ошибки документа DepEnergo2010
                    if (columnCells.Count == 1 && cell.MergedRowsCount == 1 && underCells.Count == 1)
                    {
                        var cellBelowName = underCells[0].GetText(true);
                        headerCanHaveSecondLevel = cellBelowName.Length < 5;
                    }
                }
                // current cell spans several columns, so the header probably occupies two rows instead of just one
                // with the second row reserved for subheaders
                else
                {
                    foreach (var underCell in underCells)
                    {
                        underCell.TextAbove = cell.Text.NormSpaces();
                        columnCells.Add(underCell);
                        texts.Add(underCell.TextAbove + "^" + underCell.Text.NormSpaces());
                    }
                    headerEndRow = Math.Max(headerEndRow, underCells[0].Row + underCells[0].MergedRowsCount);
                }
            }
            Logger.Debug("column titles: " + string.Join("|", texts));
            return columnCells;
        }

        static public void MapStringsToConstants(IAdapter adapter, List<Cell> cells, ColumnOrdering columnOrdering)
        {
            foreach (var cell in cells)
            {
                var text = cell.GetText(true);
                Logger.Debug(string.Format("column title: \"{0}\"[{1}]",text.ReplaceEolnWithSpace().CoalesceWhitespace(), cell.CellWidth));
                DeclarationField field;
                var clean_text = AbsenceMarkers.Aggregate(text, (x, y) => x.Replace(y, "")).Trim();

                if (adapter.GetRowsCount() == cell.MergedRowsCount)
                {
                    continue;
                }

                if ((text?.Length == 0 || clean_text.Length <= 1) && (text != "№"))
                {
                    // too short title, try to predict by values
                    field = ColumnPredictor.PredictEmptyColumnTitle(adapter, cell);
                    Logger.Debug("Predict: " + field.ToString());
                }
                else {
                    if (cell.TextAbove != null)
                    {
                        text = cell.TextAbove + " " + text;
                    }
                    field = HeaderHelpers.TryGetField(text.Replace('\n', ' '));
                    if ((field == DeclarationField.None) && clean_text.Length <= 4)
                    {
                        field = ColumnPredictor.PredictEmptyColumnTitle(adapter, cell);
                        Logger.Debug("Predict: " + field.ToString());
                    }
                    if (field == DeclarationField.None) {
                        throw new SmartParserException(string.Format("Cannot recognize field \"{0}\"", text.Replace('\n', ' ')));
                    }
                }

                if (field == DeclarationField.None && !DataHelper.IsEmptyValue(text) )
                {
                    throw new ColumnDetectorException(string.Format("Fail to detect column type row: {0} title:{1}", cell.Row, text));
                }
                if (ColumnPredictor.CalcPrecision)
                {
                    ColumnPredictor.PredictForPrecisionCheck(adapter, cell, field);
                }

                AddColumn(columnOrdering, field, cell);
                if (ColumnOrdering.SearchForFioColumnOnly)
                {
                    if  (field == DeclarationField.NameAndOccupationOrRelativeType ||
                         field == DeclarationField.NameOrRelativeType)
                    {
                        break;
                    }
                }
            }
        }

        static public void ReadHeader(IAdapter adapter, int headerStartRow, ColumnOrdering columnOrdering)
        {
            var cells = GetColumnCells(adapter, headerStartRow, out var headerEndRow);
            MapStringsToConstants(adapter, cells, columnOrdering);

            columnOrdering.HeaderBegin = headerStartRow;
            columnOrdering.HeaderEnd = headerEndRow;
            var firstDataRow = columnOrdering.HeaderEnd.Value;

            // пропускаем колонку с номерами
            if (firstDataRow < adapter.GetRowsCount())
            {
                var cellText1 = adapter.GetCell(firstDataRow, 0).GetText();
                var cellText2 = adapter.GetCell(firstDataRow, 1).GetText();
                if (cellText1 == "1" && cellText2 == "2")
                {
                    firstDataRow++;
                }
            }

            columnOrdering.FirstDataRow = firstDataRow;

            if (columnOrdering.ColumnOrder.Count == 0)
            {
                throw new SmartParserException("cannot find headers");
            }
            // todo check whether we need them
            FixMissingSubheadersForMixedRealEstate(adapter, columnOrdering);
            FixMissingSubheadersForVehicle(adapter, columnOrdering);
            FixBadColumnName01(columnOrdering);
            FixBadColumnName02(columnOrdering);
            columnOrdering.FinishOrderingBuilding(cells[0].AdditTableIndention);
        }
    }
}
