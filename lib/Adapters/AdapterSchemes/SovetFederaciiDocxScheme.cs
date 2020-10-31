﻿using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Parser.Lib;
using Smart.Parser.Lib.Adapters.DocxSchemes;
using TI.Declarator.ParserCommon;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using EP.Ner;
using EP.Morph;

namespace Smart.Parser.Lib.Adapters.AdapterSchemes
{
    internal class SovetFederaciiDocxScheme : IAdapterScheme
    {
        public override bool CanProcess(WordprocessingDocument document)
        {
            var docPart = document.MainDocumentPart;
            var tables = docPart.Document.Descendants<Table>().ToList();

            var paragraphs = docPart.Document.Descendants<Paragraph>().ToList();
            var titles = paragraphs.FindAll(x => x.InnerText.Contains("раздел", System.StringComparison.OrdinalIgnoreCase));

            if (titles.Count == 0)
            {
                return false;
            }

            if (!titles[0].InnerText.Contains("Сведения о доходах"))
            {
                return false;
            }

            if (!titles[1].InnerText.Contains("Сведения об имуществе"))
            {
                return false;
            }

            var firstTableTitlesOk = tables.Any(
                x => x.Descendants<TableRow>().Any(
                    y => y.InnerText.OnlyRussianLowercase().Contains("ппвиддоходавеличинадоходаруб")));

            return firstTableTitlesOk;
        }

        public override Declaration Parse(Parser parser, int? userDocumentFileId)
        {
            InitializeEP();

            var columnOrdering = new ColumnOrdering();
            var declaration = parser.InitializeDeclaration(columnOrdering, userDocumentFileId);
            declaration.Properties.Year = this.GetYear();
            declaration.Properties.SheetTitle = this.FindTitleAboveTheTable();

            var currentDeclarant = this.CreatePublicServant(columnOrdering);
            declaration.PublicServants.Add(currentDeclarant);

            var tables = this.Document.Descendants<Table>().ToList();

            var lastTableProcessor = string.Empty;
            Table lastTable;

            foreach (var table in tables)
            {
                var rows = table.Descendants<TableRow>().ToList();
                if (rows.Count == 0)
                {
                    continue;
                }

                var cells = rows[0].Descendants<TableCell>().ToList();
                var rowText = rows[0].InnerText.OnlyRussianLowercase();
                var firstCellText = cells[0].InnerText.OnlyRussianLowercase();

                if (firstCellText == "замещаемаядолжность")
                {
                    this.ProcessPositionTable(table, currentDeclarant);
                    lastTableProcessor = "Position";
                }
                else if (rowText.Contains("ппвиддоходавеличинадоходаруб"))
                {
                    this.ProcessIncomeTable(table, currentDeclarant);
                    lastTableProcessor = "Income";
                }
                else if (rowText.ContainsAny("ппвидимущества", "собственникимущества"))
                {
                    this.ParseRealEstateTable(table, currentDeclarant, RealtyParser.OwnedString);
                    lastTableProcessor = "RealEstateOwned";
                }
                else if (rowText.ContainsAny("ппвидимущества", "находитсявпользовании"))
                {
                    this.ParseRealEstateTable(table, currentDeclarant, RealtyParser.StateString);
                    lastTableProcessor = "RealEstateState";
                }
                else if (rowText.Contains("видимаркатранспорт") &&
                         rowText.Contains("собственник"))
                {
                    this.ParseVehicleTable(table, currentDeclarant);
                    lastTableProcessor = "Vehicle";
                }
                else
                {
                    switch (lastTableProcessor)
                    {
                        case "Vehicle": this.ParseVehicleTable(table, currentDeclarant); break;
                        case "RealEstateState": this.ParseRealEstateTable(table, currentDeclarant, RealtyParser.StateString); break;
                        case "RealEstateOwned": this.ParseRealEstateTable(table, currentDeclarant, RealtyParser.OwnedString); break;
                    }
                }

                lastTable = table;
            }

            return declaration;
        }

        private static void InitializeEP()
        {
            // инициализация - необходимо проводить один раз до обработки текстов
            Logger.Info("Initializing EP... ");

            ProcessorService.Initialize(MorphLang.RU | MorphLang.EN);
            // инициализируются все используемые анализаторы
            EP.Ner.Money.MoneyAnalyzer.Initialize();
            EP.Ner.Uri.UriAnalyzer.Initialize();
            EP.Ner.Phone.PhoneAnalyzer.Initialize();
            EP.Ner.Definition.DefinitionAnalyzer.Initialize();
            EP.Ner.Date.DateAnalyzer.Initialize();
            EP.Ner.Bank.BankAnalyzer.Initialize();
            EP.Ner.Geo.GeoAnalyzer.Initialize();
            EP.Ner.Address.AddressAnalyzer.Initialize();
            EP.Ner.Org.OrganizationAnalyzer.Initialize();
            EP.Ner.Person.PersonAnalyzer.Initialize();
            EP.Ner.Mail.MailAnalyzer.Initialize();
            EP.Ner.Transport.TransportAnalyzer.Initialize();
            EP.Ner.Decree.DecreeAnalyzer.Initialize();
            EP.Ner.Titlepage.TitlePageAnalyzer.Initialize();
            EP.Ner.Booklink.BookLinkAnalyzer.Initialize();
            EP.Ner.Named.NamedEntityAnalyzer.Initialize();
            EP.Ner.Goods.GoodsAnalyzer.Initialize();
        }

        private void ParseVehicleTable(Table table, PublicServant person)
        {
            var rows = table.Descendants<TableRow>().ToList().Skip(1);
            var currentVehicleType = string.Empty;
            foreach (var row in rows)
            {
                var cells = row.Descendants<TableCell>().ToList();
                var text = cells[0].InnerText;

                var gridSpan = cells[0].TableCellProperties.GetFirstChild<GridSpan>();
                var mergedColsCount = (gridSpan == null) ? 1 : (int)gridSpan.Val;
                if (mergedColsCount > 1)
                {
                    currentVehicleType = text;
                    continue;
                }

                var textStr = cells[1].InnerText;
                if (textStr.OnlyRussianLowercase() == "неимеет")
                {
                    continue;
                }

                var ownerStr = cells[2].InnerText;
                var owners = ownerStr.Split(",").ToList();

                foreach (var owner in owners)
                {
                    var vehicle = new Vehicle(textStr, currentVehicleType);

                    var relationType = DataHelper.ParseRelationType(owner, false);
                    if (DataHelper.IsRelativeInfo(owner))
                    {
                        var relative = this.GetPersonRelative(person, relationType);
                        relative.Vehicles.Add(vehicle);
                    }
                    else
                    {
                        person.Vehicles.Add(vehicle);
                    }
                }
            }
        }

        private void ProcessPositionTable(Table table, PublicServant currentDeclarant)
        {
            var rows = table.Descendants<TableRow>().ToList();
            var cells = rows[0].Descendants<TableCell>().ToList();
            currentDeclarant.Occupation = cells[1].InnerText;
        }

        private void ProcessIncomeTable(Table table, PublicServant person)
        {
            var rows = table.Descendants<TableRow>().ToList().Skip(1);
            foreach (var row in rows)
            {
                var cells = row.Descendants<TableCell>().ToList();
                var incomeType = cells[1].InnerText.OnlyRussianLowercase();
                if (incomeType.StartsWith("декларированныйгодовойдоход"))
                {
                    var incomeRaw = cells[2].InnerText.Trim();
                    if (incomeRaw?.Length == 0)
                    {
                        continue;
                    }

                    person.DeclaredYearlyIncomeRaw = incomeRaw;
                    person.DeclaredYearlyIncome = DataHelper.ParseDeclaredIncome(incomeRaw, false);
                }
                else if (DataHelper.IsRelativeInfo(incomeType))
                {
                    var incomeRaw = cells[2].InnerText.Trim();
                    var relationType = DataHelper.ParseRelationType(incomeType, false);
                    var relative = new Relative
                    {
                        RelationType = relationType,
                        DeclaredYearlyIncomeRaw = incomeRaw,
                        DeclaredYearlyIncome = DataHelper.ParseDeclaredIncome(incomeRaw, false),
                    };
                    person.AddRelative(relative);
                }
            }
        }

        private void ParseRealEstateTable(Table table, PublicServant person, string ownTypeByColumn)
        {
            var rows = table.Descendants<TableRow>().ToList().Skip(1);
            var currentRealEstateType = string.Empty;
            foreach (var row in rows)
            {
                var cells = row.Descendants<TableCell>().ToList();
                var gridSpan = cells[0].TableCellProperties.GetFirstChild<GridSpan>();
                var mergedColsCount = (gridSpan == null) ? 1 : (int)gridSpan.Val;
                var text = cells[0].InnerText;

                if (mergedColsCount > 1)
                {
                    currentRealEstateType = text;
                    continue;
                }

                var textStr = cells[1].InnerText;
                if (textStr.OnlyRussianLowercase() == "неимеет")
                {
                    continue;
                }

                var areaStr = cells[2].InnerText;
                var countryStr = cells[3].InnerText;
                var ownerStr = cells[4].InnerText;

                var owners = ownerStr.Split(",").ToList();
                var shares = owners.Where(x => x.Contains(" доля") || x.Contains(" доли")).ToList();
                owners = owners.Where(x => !(x.Contains(" доля") || x.Contains(" доли"))).ToList();

                if (shares.Count != owners.Count && shares.Count > 0)
                {
                    throw new SmartParserException("shares.Count != owners.Count in SovetFederaciiDocxScheme");
                }

                if (shares.Count < owners.Count)
                {
                    shares = Enumerable.Repeat<string>(null, owners.Count - shares.Count).ToList();
                }

                var zippedOwners = owners.Zip(shares);

                foreach (var (owner, share) in zippedOwners)
                {
                    var realEstateProperty = new RealEstateProperty
                    {
                        Text = textStr,
                        square = DataHelper.ParseSquare(areaStr),
                        type_raw = currentRealEstateType,
                        square_raw = ParserBase.NormalizeRawDecimalForTest(areaStr),
                        country_raw = DataHelper.ParseCountry(countryStr),
                        own_type_by_column = ownTypeByColumn,
                    };

                    if (share != default)
                    {
                        realEstateProperty.own_type_raw = share;
                    }

                    var relationType = DataHelper.ParseRelationType(owner, false);
                    if (DataHelper.IsRelativeInfo(owner))
                    {
                        var relative = this.GetPersonRelative(person, relationType);
                        relative.RealEstateProperties.Add(realEstateProperty);
                    }
                    else
                    {
                        person.RealEstateProperties.Add(realEstateProperty);
                    }
                }
            }
        }

        private Relative GetPersonRelative(PublicServant person, RelationType relationType)
        {
            var foundRelatives = person.Relatives.Where(x => x.RelationType == relationType).ToList();
            if (foundRelatives.Count != 0)
            {
                return foundRelatives[0];
            }

            var relative = new Relative
            {
                RelationType = relationType,
            };
            person.AddRelative(relative);
            return relative;
        }

        private PublicServant CreatePublicServant(ColumnOrdering columnOrdering)
        {
            var currentDeclarant = new PublicServant
            {
                NameRaw = this.GetPersonName(),
                Ordering = columnOrdering,
                Index = 1,
            };
            return currentDeclarant;
        }

        public string FindTitleAboveTheTable()
        {
            var title = string.Empty;
            var body = this.Document.Body;
            foreach (var p in this.Document.Descendants<Paragraph>())
            {
                if (p.Parent != body)
                {
                    break;
                }

                title += p.InnerText + "\n";
            }

            return title.Trim().Replace("\n", " ");
        }

        private int GetYear()
        {
            var paragraphs = this.Document.Descendants<Paragraph>().ToList();
            var p = paragraphs.Find(x => x.InnerText.StartsWith("Сведения представлены за"));
            var decemberYearMatches = Regex.Matches(p.InnerText, @"(31\s+декабря\s+)(20\d\d)(\s+года)");
            var year = 0;
            if (decemberYearMatches.Count > 0)
            {
                year = int.Parse(decemberYearMatches[0].Groups[2].Value);
            }

            return year;
        }

        private string GetPersonName()
        {
            var paragraphs = this.Document.Descendants<Paragraph>().ToList();
            var txt = this.FindTitleAboveTheTable();

            // создаём экземпляр обычного процессора
            using (var proc = ProcessorService.CreateProcessor())
            {
                // анализируем текст
                var ar = proc.Process(new SourceOfAnalysis(txt));
                var nameEntity = ar.Entities.ToList().Find(x => x.TypeName == "PERSON");
                if (nameEntity != default)
                {
                    var t = nameEntity.ToString().Split(" ");
                    return $"{t[2]} {t[0]} {t[1]}";
                }
            }

            return default;
        }
    }
}