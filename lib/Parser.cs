using Parser.Lib;
using Smart.Parser.Adapters;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using TI.Declarator.ParserCommon;

namespace Smart.Parser.Lib
{
    public class Parser : RealtyParser
    {
        private DateTime FirstPassStartTime;
        private DateTime SecondPassStartTime;
        private readonly bool FailOnRelativeOrphan;

        public int NameOrRelativeTypeColumn { get; set; } = 1;

        public Parser(IAdapter adapter, bool failOnRelativeOrphan = true)
        {
            this.Adapter = adapter;
            this.FailOnRelativeOrphan = failOnRelativeOrphan;
            ParserNumberFormatInfo.NumberDecimalSeparator = ",";
        }

        public static void InitializeSmartParser()
        {
            Smart.Parser.Adapters.AsposeLicense.SetAsposeLicenseFromEnvironment();

            var culture = new System.Globalization.CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = culture;
            var envVars = Environment.GetEnvironmentVariables();
            if (envVars.Contains("DECLARATOR_CONV_URL"))
            {
                IAdapter.ConvertedFileStorageUrl = envVars["DECLARATOR_CONV_URL"].ToString();
            }
        }

        public Declaration InitializeDeclaration(ColumnOrdering columnOrdering, int? user_documentfile_id)
        {
            // parse filename
            var result = DataHelper.ParseDocumentFileName(this.Adapter.DocumentFile, out var documentfile_id, out var archive);
            if (user_documentfile_id.HasValue)
            {
                documentfile_id = user_documentfile_id;
            }

            var properties = new DeclarationProperties()
            {
                SheetTitle = columnOrdering.Title,
                Year = columnOrdering.Year,
                DocumentFileId = documentfile_id,
                ArchiveFileName = archive,
                SheetNumber = this.Adapter.GetWorksheetIndex(),
            };
            if (properties.Year == null)
            {
                properties.Year = columnOrdering.YearFromIncome;
            }

            var declaration = new Declaration()
            {
                Properties = properties,
            };
            return declaration;
        }

        private class TBorderFinder
        {
            private DeclarationSection CurrentSection = null;
            private PublicServant CurrentDeclarant = null;
            public Person CurrentPerson = null;
            private readonly Declaration _Declaration;
            private readonly bool FailOnRelativeOrphan;

            public TBorderFinder(Declaration declaration, bool failOnRelativeOrphan)
            {
                this._Declaration = declaration;
                this.FailOnRelativeOrphan = failOnRelativeOrphan;
            }

            public void FinishDeclarant()
            {
                this.CurrentDeclarant = null;
                this.CurrentPerson = null;
            }

            public void CreateNewSection(int row, string sectionTitle)
            {
                this.CurrentSection = new DeclarationSection() { Row = row, Name = sectionTitle };
                Logger.Debug($"find section at line {row}:'{sectionTitle}'");
                this.FinishDeclarant();
            }

            // see 8562.pdf.docx  in tests
            //  calc string width using graphics.MeasureString methods
            private bool DivideDeclarantAndRelativesBySoftEolns(ColumnOrdering columnOrdering, DataRow row)
            {
                if (this.CurrentDeclarant.Relatives.Any())
                {
                    return false;
                }

                if (!columnOrdering.ContainsField(DeclarationField.NameOrRelativeType))
                {
                    return false;
                }

                var nameCell = row.GetDeclarationField(DeclarationField.NameOrRelativeType);
                if (!(nameCell is OpenXmlWordCell) && !(nameCell is HtmlAdapterCell))
                {
                    return false;
                }

                if (nameCell is null)
                {
                    return false;
                }

                if (nameCell.IsEmpty)
                {
                    return false;
                }

                if (nameCell.FontSize == 0)
                {
                    return false; // no font info
                }

                var lines = nameCell.GetLinesWithSoftBreaks();
                if (lines.Count < 2)
                {
                    return false;
                }

                var borders = new List<int>() { 0 };

                for (var i = 1; i < lines.Count; ++i)
                {
                    if (DataHelper.ParseRelationType(lines[i], false) != RelationType.Error)
                    {
                        borders.Add(i);
                    }
                }

                if (borders.Count == 1)
                {
                    return false;
                }

                var dividedLines = new List<DataRow>();
                for (var i = 0; i < borders.Count; ++i)
                {
                    dividedLines.Add(row.DeepClone());
                }

                for (var i = 0; i < row.Cells.Count; ++i)
                {
                    var divided = row.Cells[i].GetLinesWithSoftBreaks();
                    var start = 0;
                    for (var k = 0; k < borders.Count; ++k)
                    {
                        var end = (k + 1 == borders.Count) ? divided.Count : borders[k + 1];
                        if (start < divided.Count)
                        {
                            var value = string.Join("\n", divided.Skip(start).Take(end - start));
                            if (value.Length > 0)
                            {
                                dividedLines[k].Cells[i].Text = value;
                                dividedLines[k].Cells[i].IsEmpty = false;
                            }
                        }

                        start = end;
                    }
                }

                for (var k = 0; k < borders.Count; ++k)
                {
                    var currRow = dividedLines[k];
                    var nameOrRelativeType = currRow.GetDeclarationField(DeclarationField.NameOrRelativeType).Text.Replace("не имеет", string.Empty);
                    if (k == 0)
                    {
                        currRow.PersonName = nameOrRelativeType;
                        currRow.Occupation = row.Occupation.Replace("не имеет", string.Empty);
                        currRow.Department = row.Department;
                        if (currRow.Department != null)
                        {
                            currRow.Department = currRow.Department.Replace("не имеет", string.Empty);
                        }

                        this.InitDeclarantProperties(currRow);
                    }
                    else
                    {
                        if (!DataHelper.IsRelativeInfo(nameOrRelativeType))
                        {
                            Logger.Error($"cannot parse relative {nameOrRelativeType.ReplaceEolnWithSpace()}");
                            return false;
                        }
                        else
                        {
                            currRow.SetRelative(nameOrRelativeType);
                        }

                        this.CreateNewRelative(currRow);
                    }

                    this.CurrentPerson.DateRows.Add(dividedLines[k]);
                }

                return true;
            }

            public void AddInputRowToCurrentPerson(ColumnOrdering columnOrdering, DataRow row)
            {
                if (this.CurrentPerson != null && !this.DivideDeclarantAndRelativesBySoftEolns(columnOrdering, row))
                {
                    this.CurrentPerson.DateRows.Add(row);
                    this.TransposeTableByRelatives(columnOrdering, row);
                }
            }

            private void CopyRelativeFieldToMainCell(DataRow row, DeclarationField relativeMask, DeclarationField f, ref DataRow childRow)
            {
                if ((f & relativeMask) > 0)
                {
                    var value = row.GetContents(f, false);
                    if (!DataHelper.IsEmptyValue(value))
                    {
                        if (childRow == null)
                        {
                            childRow = row.DeepClone();
                        }

                        f = (f & ~relativeMask) | DeclarationField.MainDeclarant;
                        var declarantCell = childRow.GetDeclarationField(f);
                        declarantCell.Text = value;
                        declarantCell.IsEmpty = false;
                    }
                }
            }

            public void TransposeTableByRelatives(ColumnOrdering columnOrdering, DataRow row)
            {
                DataRow childRow = null;
                DataRow spouseRow = null;
                foreach (var f in columnOrdering.ColumnOrder.Keys)
                {
                    this.CopyRelativeFieldToMainCell(row, DeclarationField.DeclarantChild, f, ref childRow);
                    this.CopyRelativeFieldToMainCell(row, DeclarationField.DeclarantSpouse, f, ref spouseRow);
                }

                if (childRow != null)
                {
                    childRow.RelativeType = "несовершеннолетний ребенок";
                    this.CreateNewRelative(childRow);
                    this.CurrentPerson.DateRows.Add(childRow);
                    Logger.Debug("Create artificial line for a child");
                }

                if (spouseRow != null)
                {
                    spouseRow.RelativeType = "супруга";
                    this.CreateNewRelative(spouseRow);
                    this.CurrentPerson.DateRows.Add(spouseRow);
                    Logger.Debug("Create artificial line for a spouse");
                }
            }

            public void InitDeclarantProperties(DataRow row)
            {
                this.CurrentDeclarant.NameRaw = row.PersonName.RemoveStupidTranslit().Replace("не имеет", string.Empty);
                this.CurrentDeclarant.Occupation = row.Occupation.Replace("не имеет", string.Empty);
                this.CurrentDeclarant.Department = row.Department;
                this.CurrentDeclarant.Ordering = row.ColumnOrdering;
            }

            public void CreateNewDeclarant(IAdapter adapter, DataRow row)
            {
                Logger.Debug("Declarant {0} at row {1}", row.PersonName, row.GetRowIndex());
                this.CurrentDeclarant = new PublicServant();
                this.InitDeclarantProperties(row);
                if (this.CurrentSection != null)
                {
                    this.CurrentDeclarant.Department = this.CurrentSection.Name;
                }

                this.CurrentDeclarant.Index = row.GetPersonIndex();

                this.CurrentPerson = this.CurrentDeclarant;
                this.CurrentPerson.document_position = row.NameDocPosition;
                this.CurrentPerson.sheet_index = this._Declaration.Properties.SheetNumber;
                this._Declaration.PublicServants.Add(this.CurrentDeclarant);
            }

            public void CreateNewRelative(DataRow row)
            {
                Logger.Debug("Relative {0} at row {1}", row.RelativeType, row.GetRowIndex());
                if (this.CurrentDeclarant == null)
                {
                    if (this.FailOnRelativeOrphan)
                    {
                        throw new SmartParserRelativeWithoutPersonException(
                            $"Relative {row.RelativeType} at row {row.GetRowIndex()} without main Person");
                    }
                    else
                    {
                        return;
                    }
                }

                var relative = new Relative();
                this.CurrentDeclarant.AddRelative(relative);
                this.CurrentPerson = relative;

                var relationType = DataHelper.ParseRelationType(row.RelativeType, false);
                if (relationType == RelationType.Error)
                {
                    throw new SmartParserException(
                        $"Wrong relative name '{row.RelativeType}' at row {row} ");
                }

                relative.RelationType = relationType;
                relative.document_position = row.NameDocPosition;
                relative.sheet_index = this._Declaration.Properties.SheetNumber;
            }
        }

        private bool IsNumbersRow(DataRow row)
        {
            var s = string.Empty;
            foreach (var c in row.Cells)
            {
                s += c.Text.Replace("\n", string.Empty).Replace(" ", string.Empty) + " ";
            }

            return s.StartsWith("1 2 3 4");
        }

        private bool IsHeaderRow(DataRow row, out ColumnOrdering columnOrdering)
        {
            columnOrdering = null;
            if (!ColumnDetector.WeakHeaderCheck(row.Cells))
            {
                return false;
            }

            try
            {
                columnOrdering = new ColumnOrdering();
                ColumnDetector.ReadHeader(this.Adapter, row.GetRowIndex(), columnOrdering);
                return true;
            }
            catch (Exception e)
            {
                Logger.Debug($"Cannot parse possible header, row={e}, error={row.GetRowIndex()}, so skip it may be it is a data row ");
            }

            return false;
        }

        public Declaration Parse(ColumnOrdering columnOrdering, bool updateTrigrams, int? documentfile_id)
        {
            this.FirstPassStartTime = DateTime.Now;

            var declaration = this.InitializeDeclaration(columnOrdering, documentfile_id);

            var rowOffset = columnOrdering.FirstDataRow;

            var borderFinder = new TBorderFinder(declaration, this.FailOnRelativeOrphan);

            if (columnOrdering.Section != null)
            {
                borderFinder.CreateNewSection(rowOffset, columnOrdering.Section);
            }

            var skipEmptyPerson = false;
            var prevPersonName = string.Empty;

            for (var row = rowOffset; row < this.Adapter.GetRowsCount(); row++)
            {
                var currRow = this.Adapter.GetRow(columnOrdering, row);
                if (currRow?.IsEmpty() != false)
                {
                    continue;
                }

                if (this.IsNumbersRow(currRow))
                {
                    continue;
                }

                Logger.Debug($"currRow {row}: {currRow.DebugString()}");

                if (IAdapter.IsSectionRow(currRow.Cells, columnOrdering.GetMaxColumnEndIndex(), false, out var sectionName))
                {
                    borderFinder.CreateNewSection(row, sectionName);
                    continue;
                }

                {
                    if (this.IsHeaderRow(currRow, out var newColumnOrdering))
                    {
                        columnOrdering = newColumnOrdering;
                        row = newColumnOrdering.GetPossibleHeaderEnd() - 1; // row++ in "for" cycle
                        continue;
                    }
                }

                if (updateTrigrams)
                {
                    ColumnPredictor.UpdateByRow(columnOrdering, currRow);
                }

                if (!currRow.InitPersonData(prevPersonName))
                {
                    // be robust, ignore errors see 8562.pdf.docx in tests
                    continue;
                }

                if (currRow.PersonName != string.Empty)
                {
                    prevPersonName = currRow.PersonName;
                    borderFinder.CreateNewDeclarant(this.Adapter, currRow);
                    if (borderFinder.CurrentPerson != null)
                    {
                        skipEmptyPerson = false;
                    }
                }
                else if (currRow.RelativeType != string.Empty)
                {
                    if (!skipEmptyPerson)
                    {
                        try
                        {
                            borderFinder.CreateNewRelative(currRow);
                        }
                        catch (SmartParserRelativeWithoutPersonException e)
                        {
                            skipEmptyPerson = true;
                            Logger.Error(e.Message);
                            continue;
                        }
                    }
                }
                else
                {
                    if (borderFinder.CurrentPerson == null && this.FailOnRelativeOrphan)
                    {
                        skipEmptyPerson = true;
                        Logger.Error($"No person to attach info on row={row}");
                        continue;
                    }
                }

                if (!skipEmptyPerson)
                {
                    borderFinder.AddInputRowToCurrentPerson(columnOrdering, currRow);
                }
            }

            if (updateTrigrams)
            {
                ColumnPredictor.WriteData();
            }

            Logger.Info("Parsed {0} declarants", declaration.PublicServants.Count);
            if (!ColumnOrdering.SearchForFioColumnOnly)
            {
                this.ParsePersonalProperties(declaration);
            }

            return declaration;
        }

        public void ForgetThousandMultiplier(Declaration declaration)
        {
            // the incomes are so high, that we should not multiply incomes by 1000 although the
            // column title specify this multiplier
            var incomes = new List<decimal>();
            foreach (var servant in declaration.PublicServants)
            {
                foreach (DataRow row in servant.DateRows)
                {
                    if (row.ColumnOrdering.ContainsField(DeclarationField.DeclaredYearlyIncomeThousands))
                    {
                        var dummy = new PublicServant();
                        this.ParseIncome(row, dummy, true);
                        if (dummy.DeclaredYearlyIncome != null)
                        {
                            incomes.Add(dummy.DeclaredYearlyIncome.Value);
                        }
                    }
                }
            }

            if (incomes.Count > 3)
            {
                incomes.Sort();
                var medianIncome = incomes[incomes.Count / 2];
                if (medianIncome > 10000)
                {
                    declaration.Properties.IgnoreThousandMultipler = true;
                }
            }
        }

        private bool ParseIncomeOneField(DataRow currRow, Person person, DeclarationField field, bool ignoreThousandMultiplier)
        {
            if (!currRow.ColumnOrdering.ContainsField(field))
            {
                return false;
            }

            var fieldStr = currRow.GetContents(field);
            if (DataHelper.IsEmptyValue(fieldStr))
            {
                return false;
            }

            var fieldInThousands = (field & DeclarationField.DeclaredYearlyIncomeThousands) > 0;
            person.DeclaredYearlyIncome = DataHelper.ParseDeclaredIncome(fieldStr, fieldInThousands);
            if (!ignoreThousandMultiplier || fieldStr.Contains("тыс."))
            {
                person.DeclaredYearlyIncome *= 1000;
            }

            if (!DataHelper.IsEmptyValue(fieldStr))
            {
                person.DeclaredYearlyIncomeRaw = NormalizeRawDecimalForTest(fieldStr);
            }

            return true;
        }

        private bool ParseIncome(DataRow currRow, Person person, bool ignoreThousandMultiplier)
        {
            try
            {
                return
                    this.ParseIncomeOneField(currRow, person, DeclarationField.DeclaredYearlyIncomeThousands, ignoreThousandMultiplier)
                    || this.ParseIncomeOneField(currRow, person, DeclarationField.DeclaredYearlyIncome, true)
                    || this.ParseIncomeOneField(currRow, person, DeclarationField.DeclarantIncomeInThousands, ignoreThousandMultiplier)
                    || this.ParseIncomeOneField(currRow, person, DeclarationField.DeclarantIncome, true);
            }
            catch (SmartParserFieldNotFoundException) when (person is Relative relative && relative.RelationType == RelationType.Child)
            {
                Logger.Info("Child's income is unparsable, set it to 0 ");
                return true;
            }
        }

        public Declaration ParsePersonalProperties(Declaration declaration)
        {
            this.ForgetThousandMultiplier(declaration);
            this.SecondPassStartTime = DateTime.Now;
            var count = 0;
            var total_count = declaration.PublicServants.Count;
            decimal totalIncome = 0;

            foreach (var servant in declaration.PublicServants)
            {
                count++;
                if (count % 1000 == 0)
                {
                    var time_sec = DateTime.Now.Subtract(this.SecondPassStartTime).TotalSeconds;
                    Logger.Info("Done: {0:0.00}%", 100.0 * count / total_count);

                    Logger.Info("Rate: {0:0.00} declarant in second", count / time_sec);
                }

                var servantAndRel = new List<Person>() { servant };
                servantAndRel.AddRange(servant.Relatives);

                foreach (var person in servantAndRel)
                {
                    if (person is PublicServant publicServant)
                    {
                        Logger.Debug("PublicServant: " + publicServant.NameRaw.ReplaceEolnWithSpace());
                    }

                    var foundIncomeInfo = false;

                    var rows = new List<DataRow>();
                    foreach (DataRow row in person.DateRows)
                    {
                        if (row == null || row.Cells.Count == 0)
                        {
                            continue;
                        }

                        if (this.Adapter.IsExcel() &&
                            !row.IsEmpty(DeclarationField.StatePropertyType,
                                DeclarationField.MixedRealEstateType,
                                DeclarationField.OwnedRealEstateType) &&
                            row.IsEmpty(DeclarationField.MixedRealEstateSquare,
                                DeclarationField.OwnedRealEstateSquare,
                                DeclarationField.StatePropertySquare,
                                DeclarationField.OwnedRealEstateCountry,
                                DeclarationField.MixedRealEstateCountry,
                                DeclarationField.StatePropertyCountry,
                                DeclarationField.NameOrRelativeType) &&
                            rows.Count > 0)
                        {
                            Logger.Debug("Merge row to the last if state and square cell is empty");
                            rows.Last().Merge(row);
                        }
                        else
                        {
                            rows.Add(row);
                        }
                    }

                    foreach (var currRow in rows)
                    {
                        if (!foundIncomeInfo && this.ParseIncome(currRow, person, declaration.Properties.IgnoreThousandMultipler))
                        {
                            totalIncome += person.DeclaredYearlyIncome ?? 0;
                            foundIncomeInfo = true;
                        }

                        this.ParseOwnedProperty(currRow, person);
                        this.ParseStateProperty(currRow, person);
                        this.ParseMixedProperty(currRow, person);

                        this.AddVehicle(currRow, person);
                    }
                }
            }

            Logger.Info("Total income: {0}", totalIncome);
            var seconds = DateTime.Now.Subtract(this.FirstPassStartTime).TotalSeconds;
            Logger.Info("Final Rate: {0:0.00} declarant in second", count / seconds);
            var total_seconds = DateTime.Now.Subtract(this.FirstPassStartTime).TotalSeconds;
            Logger.Info("Total time: {0:0.00} seconds", total_seconds);
            return declaration;
        }

        private void AddVehicle(DataRow r, Person person)
        {
            if (r.ColumnOrdering.ColumnOrder.ContainsKey(DeclarationField.Vehicle))
            {
                var s = r.GetContents(DeclarationField.Vehicle).Replace("не имеет", string.Empty);
                if (!DataHelper.IsEmptyValue(s))
                {
                    person.Vehicles.Add(new Vehicle(s));
                }
            }
            else if (r.ColumnOrdering.ColumnOrder.ContainsKey(DeclarationField.DeclarantVehicle))
            {
                var s = r.GetContents(DeclarationField.DeclarantVehicle).Replace("не имеет", string.Empty);
                if (!DataHelper.IsEmptyValue(s))
                {
                    person.Vehicles.Add(new Vehicle(s));
                }
            }
            else
            {
                var t = r.GetContents(DeclarationField.VehicleType).Replace("не имеет", string.Empty);
                var m = r.GetContents(DeclarationField.VehicleModel, false).Replace("не имеет", string.Empty);
                var text = t + " " + m;
                if (t == m)
                {
                    text = t;
                    m = string.Empty;
                }

                if (!DataHelper.IsEmptyValue(m) || !DataHelper.IsEmptyValue(t))
                {
                    person.Vehicles.Add(new Vehicle(text.Trim(), t, m));
                }
            }
        }

        public IAdapter Adapter { get; set; }
    }
}
