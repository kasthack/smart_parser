﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Smart.Parser.Adapters;
using Smart.Parser.Lib;
using System.IO;
using Parser.Lib;
using TI.Declarator.DeclaratorApiClient;
using TI.Declarator.ParserCommon;
using TI.Declarator.JsonSerialization;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Smart.Parser
{
    public class SmartParserVersions
    {
        public List<Version> versions = new List<Version>();

        public class Version
        {
            public string id = "";
            public string info = "";
        }
    }
    public enum AdapterFamily
    {
        aspose,
        npoi,
        microsoft,
        prod,
    }
    public static class Program
    {
        public static Options CurrentOptions { get; private set; } = new Options();

        public class Options
        {
            public string OutputFile { get; set; }
            public string License { get; set; }
            public string Log { get; set; }
            public bool SkipLogging { get; set; }
            public Logger.LogLevel Verbose { get; set; }
            public bool Columnsonly { get; set; }
            public bool Checkjson { get; set; }
            public AdapterFamily Adapter { get; set; } = AdapterFamily.prod;
            public int MaxRows { get; set; } = -1;
            public DeclarationField DumpColumn { get; set; } = DeclarationField.None;
            public string DumpHtml { get; set; }
            public string Toloka { get; set; }
            public bool SkipRelativeOrphan { get; set; }
            public bool ApiValidation { get; set; }
            public bool BuildTrigrams { get; set; }
            public bool CheckPredictor { get; set; }
            public int? DocfileId { get; set; }
            public string ConvertedStorageUrl { get; set; }
            public bool FioOnly { get; set; }
            public bool DecimalRawNormalization { get; set; }
            public bool Disclosures { get; set; }
            public string InputFile { get; set; }
        }

        private static RootCommand BuildOptions() => new RootCommand
            {
                new Option<string>(new[] { "--outputFile", "-o" }, "use file for output"),
                new Option<string>("-license", ""),
                new Option<string>("-log", ""),
                new Option<bool>("-skip-logging", ""),
                new Option<Logger.LogLevel>(new[]{ "--verbose", "-v" }, () => Logger.LogLevel.Info, "verbose level"),
                new Option<bool>("-columnsonly", ""),
                new Option<bool>("-checkjson", ""),
                new Option<AdapterFamily>("-adapter", () => AdapterFamily.prod, "can be aspose,npoi, microsoft or prod, by default is aspose"),
                new Option<int>("-max-rows", () => -1, "max rows to process from the input file"),
                new Option<DeclarationField>("-dump-column", ()=> DeclarationField.None, "dump column identified by enum DeclarationField and exit"),
                new Option<string>("-dump-html", "dump table to html"),
                new Option<string>("-toloka", "generate toloka html"),
                new Option<bool>("-skip-relative-orphan", ""),
                new Option<bool>("-api-validation", "validate JSON output by API call"),
                new Option<bool>("-build-trigrams", "build trigrams"),
                new Option<bool>("-check-predictor", "calc predictor precision"),
                new Option<string>("-docfile-id", ()=> null, "document id to initialize document/documentfile_id"),
                new Option<string>("-converted-storage-url", "document id to initialize document/documentfile_id for example http://disclosures.ru:8091, the default value is read from env variable DECLARATOR_CONV_URL"),
                new Option<bool>("-fio-only", ""),
                new Option<bool>("-decimal-raw-normalization", "print raw floats in Russian traditional format"),
                new Option<bool>("-disclosures", "use disclosures output format: save sheet id to each each section, do not produce many output files but one"),
                new Argument<string>("inputFile")
                {
                    Arity = ArgumentArity.ExactlyOne,
                },
                //version is handled automatically by .net
                //help is handled automatically
            };

        private static void Configure(Options options)
        {
            Logger.Setup(string.IsNullOrEmpty(options.Log) ? "" : Path.GetFullPath(options.Log), options.SkipLogging);
            Logger.SetLoggingLevel(options.Verbose);

            if (!string.IsNullOrEmpty(options.License))
            {
                AsposeLicense.SetLicense(options.License);
                if (!AsposeLicense.Licensed)
                {
                    throw new SmartParserException($"Not valid aspose licence {options.License}");
                }
            }

            if (string.IsNullOrEmpty(options.ConvertedStorageUrl))
            {
                IAdapter.ConvertedFileStorageUrl = options.ConvertedStorageUrl;
            }
            if (options.Disclosures)
            {
                DeclarationSerializer.SmartParserJsonFormat = SmartParserJsonFormatEnum.Disclosures;
            }
            Smart.Parser.Lib.Parser.InitializeSmartParser();
            Smart.Parser.Lib.Parser.UseDecimalRawNormalization = options.DecimalRawNormalization;
            ColumnOrdering.SearchForFioColumnOnly = options.FioOnly;
            ColumnPredictor.CalcPrecision = options.CheckPredictor;

            options.OutputFile = string.IsNullOrEmpty(options.OutputFile) ? BuildOutFileNameByInput(options.InputFile) : Path.GetFullPath(options.OutputFile);

            CurrentOptions = options;
        }
        public static string BuildOutFileNameByInput(string inputFile) => Path.Combine(Path.GetDirectoryName(inputFile), $"{Path.GetFileName(inputFile)}.json");

        private const string SupportedFileTypesPattern = "*.pdf, *.xls, *.xlsx, *.doc, *.docx";

        public static int ParseDirectory(string dirName)
        {
            var files = Directory.GetFiles(dirName, SupportedFileTypesPattern);
            return ParseMultipleFiles(Directory.GetFiles(dirName), dirName);
        }

        public static int ParseByFileMask(string fileMask)
        {
            string[] files = null;
            if (fileMask.StartsWith("@"))
            {
                var fileName = fileMask[1..];
                Logger.Info($"Reading files list from {fileName}");

                files = File.ReadAllLines(fileName).ToArray();
            }
            else
            {
                Logger.Info($"Parsing files by mask {fileMask}");

                files = Directory.GetFiles(Path.GetDirectoryName(fileMask), Path.GetFileName(fileMask),
                    SearchOption.AllDirectories);
            }

            Logger.Info("Found {0} files", files.Length);

            return ParseMultipleFiles(files, Path.GetDirectoryName(fileMask));
        }

        public static int ParseMultipleFiles(IEnumerable<string> files, string outputDir)
        {
            var parse_results = new[] { "ok", "error", "too_many_errors", "exception" }
                .ToDictionary(a => a, _ => new List<string>());

            foreach (var file in files)
            {
                Logger.Info($"Parsing file {file}");
                var caught = false;
                try
                {
                    Logger.SetOutSecond();
                    ParseFile(file, BuildOutFileNameByInput(file));
                }
                catch (SmartParserException e)
                {
                    caught = true;
                    Logger.Error($"Parsing Exception {e}");
                    parse_results["exception"].Add(file);
                }
                catch (Exception e)
                {
                    caught = true;
                    Logger.Error($"Parsing Exception {e}");
                    Logger.Debug($"Stack: {e.StackTrace}");
                    parse_results["exception"].Add(file);
                }
                finally
                {
                    Logger.SetOutMain();
                }

                if (caught)
                {
                    Logger.Info("Result: Exception");
                }
                else
                {
                    var hasErrors = Logger.Errors.Count > 0;
                    Logger.Info($"Result: {(hasErrors ? "error" : "OK")}");
                    parse_results[hasErrors ? "error" : "ok"].Add(file);
                }

                if (Logger.Errors.Count > 0)
                {
                    Logger.Info($" Parsing errors ({Logger.Errors.Count})");

                    foreach (var e in Logger.Errors)
                    {
                        Logger.Info(e);
                    }

                    Logger.Errors.Clear();
                }
            }

            Logger.Info("Parsing Results:");

            foreach (var key_value in parse_results)
            {
                Logger.Info($"Result: {key_value.Key} ({key_value.Value.Count})");
                foreach (var file in key_value.Value)
                {
                    Logger.Info(file);
                }
            }

            if (Logger.UnknownRealEstate.Count > 0)
            {
                Logger.Info("UnknownRealEstate.Count: {0}", Logger.UnknownRealEstate.Count);
                var content = string.Join("\n", Logger.UnknownRealEstate);
                var dictfile = Path.Combine(outputDir, "UnknownRealEstate.txt");
                File.WriteAllText(dictfile, content);
                Logger.Info("Output UnknownRealEstate to file {0}", dictfile);
            }

            if (ColumnPredictor.CalcPrecision)
            {
                Logger.Info(ColumnPredictor.GetPrecisionStr());
            }

            return 0;
        }

        private static IAdapter GetAdapter(string inputFile)
        {
            var extension = Path.GetExtension(inputFile).ToLowerInvariant();
            return extension switch
            {
                ".htm" or ".html" => inputFile switch
                {
                    _ when HtmAdapter.CanProcess(inputFile) => new HtmAdapter(inputFile),
                    _ => new AngleHtmlAdapter(inputFile, CurrentOptions.MaxRows),
                },
                ".pdf" or ".xhtml" or ".doc" or ".rtf" or ".toloka_json" or ".docx" => CurrentOptions.Adapter switch
                {
                    AdapterFamily.prod => OpenXmlWordAdapter.CreateAdapter(inputFile, CurrentOptions.MaxRows),
                    AdapterFamily.aspose when !AsposeLicense.Licensed => throw new Exception("doc and docx file format is not supported"),
                    _ => AsposeDocAdapter.CreateAdapter(inputFile),
                },
                ".xls" or ".xlsx" => CurrentOptions.Adapter switch
                {
                    AdapterFamily.aspose or AdapterFamily.prod => extension switch
                    {
                        _ when AsposeLicense.Licensed => AsposeExcelAdapter.CreateAdapter(inputFile, CurrentOptions.MaxRows),
                        ".xls" => throw new Exception("xls file format is not supported"),
                        _ => throw new Exception($"Cannot find adapter for {inputFile}"),
                    },
                    AdapterFamily.npoi => NpoiExcelAdapter.CreateAdapter(inputFile, CurrentOptions.MaxRows),
                    _ => throw new Exception($"Cannot find adapter for {inputFile}"),
                },
                _ => throw new NotSupportedException($"Unknown file extension {extension}")
            };
        }

        private static void DumpColumn(IAdapter adapter, ColumnOrdering columnOrdering, DeclarationField columnToDump)
        {
            var rowOffset = columnOrdering.FirstDataRow;
            for (var row = rowOffset; row < adapter.GetRowsCount(); row++)
            {
                var currRow = adapter.GetRow(columnOrdering, row);
                var cell = currRow.GetDeclarationField(columnToDump);
                var s = (cell == null) ? "null" : cell.GetText();
                s = s.Replace("\n", "\\n");
                Console.WriteLine(s);
            }
        }

        public static string CalculateMD5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string BuildInputFileId(IAdapter adapter, string filename) => $"{CalculateMD5(filename)}_{adapter.GetWorksheetName()}";

        public static void SaveRandomPortionToToloka(IAdapter adapter, ColumnOrdering columnOrdering,
            Declaration declaration, string inputFileName)
        {
            if (string.IsNullOrEmpty(CurrentOptions.Toloka))
            {
                return;
            }

            var fileID = BuildInputFileId(adapter, inputFileName);
            using (var file = new StreamWriter(CurrentOptions.Toloka))
            {
                file.WriteLine("INPUT:input_id\tINPUT:input_json\tGOLDEN:declaration_json\tHINT:text");
                var random = new Random();
                var dataRowsCount = Math.Min(20, adapter.GetRowsCount() - columnOrdering.GetPossibleHeaderEnd());
                var dataStart = random.Next(columnOrdering.GetPossibleHeaderEnd(),
                    adapter.GetRowsCount() - dataRowsCount);
                var dataEnd = dataStart + dataRowsCount;
                var json = adapter.TablePortionToJson(columnOrdering, dataStart, dataEnd);
                json.InputFileName = inputFileName;
                json.Title = declaration.Properties.SheetTitle;
                var jsonStr = JsonConvert.SerializeObject(json);
                jsonStr = jsonStr.Replace("\t", " ").Replace("\\t", " ").Replace("\"", "\"\"");
                var id = $"{fileID}_{dataStart}_{dataEnd}";
                file.WriteLine($"{id}\t\"{jsonStr}\"\t\t");
            }
        }

        private static Declaration BuildDeclarations(IAdapter adapter, string inputFile)
        {
            Declaration declaration;
            var inputFileName = Path.GetFileName(inputFile);
            var parser = new Lib.Parser(adapter, !CurrentOptions.SkipRelativeOrphan);

            if (adapter.CurrentScheme == default)
            {
                var columnOrdering = ColumnDetector.ExamineTableBeginning(adapter);

                // Try to extract declaration year from file name if we weren't able to get it from document title
                if (!columnOrdering.Year.HasValue)
                {
                    columnOrdering.Year = TextHelpers.ExtractYear(inputFileName);
                }

                Logger.Info("Column ordering: ");
                foreach (var ordering in columnOrdering.ColumnOrder)
                {
                    Logger.Info(ordering.ToString());
                }

                Logger.Info($"OwnershipTypeInSeparateField: {columnOrdering.OwnershipTypeInSeparateField}");

                if (CurrentOptions.Columnsonly)
                {
                    return null;
                }

                if (CurrentOptions.DumpColumn != DeclarationField.None)
                {
                    DumpColumn(adapter, columnOrdering, CurrentOptions.DumpColumn);
                    return null;
                }

                if (columnOrdering.Title != null)
                {
                    Logger.Info($"Declaration Title: {columnOrdering.Title} ");
                }

                if (columnOrdering.Year != null)
                {
                    Logger.Info($"Declaration Year: {columnOrdering.Year.Value} ");
                }

                if (columnOrdering.MinistryName != null)
                {
                    Logger.Info($"Declaration Ministry: {columnOrdering.MinistryName} ");
                }

                if (!(columnOrdering.ContainsField(DeclarationField.NameOrRelativeType) ||
                      columnOrdering.ContainsField(DeclarationField.NameAndOccupationOrRelativeType)))
                {
                    // TODO сначала поискать первый section_row и проверить, именно там может быть ФИО
                    // https://declarator.org/admin/declarations/jsonfile/186842/change/
                    Logger.Error("Insufficient fields: No any of Declarant Name fields found.");
                    return null;
                }

                if (!(columnOrdering.ContainsField(DeclarationField.DeclarantIncome) ||
                      columnOrdering.ContainsField(DeclarationField.DeclarantIncomeInThousands) ||
                      columnOrdering.ContainsField(DeclarationField.DeclaredYearlyIncome) ||
                      columnOrdering.ContainsField(DeclarationField.DeclaredYearlyIncomeThousands)))
                {
                    if (!ColumnOrdering.SearchForFioColumnOnly)
                    {
                        Logger.Error("Insufficient fields: No any of Declarant Income fields found.");
                        return null;
                    }
                }

                declaration = parser.Parse(columnOrdering, CurrentOptions.BuildTrigrams, CurrentOptions.DocfileId);
                SaveRandomPortionToToloka(adapter, columnOrdering, declaration, inputFile);
            }
            else
            {
                declaration = adapter.CurrentScheme.Parse(parser, CurrentOptions.DocfileId);
            }
            return declaration;
        }

        private static string DumpDeclarationsToJson(string inputFile, Declaration declaration)
        {
            string schema_errors = null;
            var output = DeclarationSerializer.Serialize(declaration, ref schema_errors);

            if (!string.IsNullOrEmpty(schema_errors))
            {
                Logger.Error($"Json schema errors:{schema_errors}");
            }
            else
            {
                Logger.Info("Json schema OK");
            }

            Logger.Info($"Output size: {output.Length}");

            if (CurrentOptions.ApiValidation)
            {
                var validationResult = ApiClient.ValidateParserOutput(output);
                if (validationResult != "[]")
                {
                    var inputFileName = Path.GetFileName(inputFile);
                    var errorsFileName = $"validation_errors_{Path.GetFileNameWithoutExtension(inputFileName)}.json";
                    var rep = MiscSerializer.DeserializeValidationReport(validationResult);
                    File.WriteAllText(errorsFileName, validationResult);
                    Logger.Error($"Api validation failed. Errors:{errorsFileName}");
                }
            }
            return output;
        }

        public static void WriteOutputJson(string inputFile, Declaration declarations, string outFile)
        {
            if (declarations != null)
            {
                var output = DumpDeclarationsToJson(inputFile, declarations);
                Logger.Info($"Writing json to {outFile}");
                File.WriteAllBytes(outFile, Encoding.UTF8.GetBytes(output));
            }
        }

        public static int ParseFile(string inputFile, string outFile)
        {
            if (CurrentOptions.Checkjson && File.Exists(outFile))
            {
                Logger.Info($"JSON file {outFile} already exist");
                return 0;
            }

            if (!File.Exists(inputFile))
            {
                Logger.Info($"ERROR: {inputFile} file NOT exists");
                return 0;
            }

            ColumnPredictor.InitializeIfNotAlready();

            var logFile = Path.Combine(Path.GetDirectoryName(inputFile), $"{Path.GetFileName(inputFile)}.log");
            Logger.SetSecondLogFileName(Path.GetFullPath(logFile));

            Logger.Info($"Parsing {inputFile}");
            var adapter = GetAdapter(inputFile);

            Logger.Info($"TablesCount = {adapter.GetTablesCount()}");
            Logger.Info($"RowsCount = {adapter.GetRowsCount()}");

            if (!inputFile.EndsWith(".toloka_json") && adapter.GetTablesCount() == 0)
            {
                throw new SmartParserException("No tables found in document");
            }

            if (CurrentOptions.DumpHtml != "")
            {
                adapter.WriteHtmlFile(CurrentOptions.DumpHtml);
            }

            if (adapter.GetWorkSheetCount() > 1)
            {
                Logger.Info($"File has multiple ({adapter.GetWorkSheetCount()}) worksheets");
                Declaration allDeclarations = null;
                for (var sheetIndex = 0; sheetIndex < adapter.GetWorkSheetCount(); sheetIndex++)
                {
                    adapter.SetCurrentWorksheet(sheetIndex);
                    try
                    {
                        if (DeclarationSerializer.SmartParserJsonFormat == SmartParserJsonFormatEnum.Disclosures)
                        {
                            var sheetDeclarations = BuildDeclarations(adapter, inputFile);
                            if (allDeclarations == null)
                            {
                                allDeclarations = sheetDeclarations;
                            }
                            else
                            {
                                allDeclarations.AddDeclarations(sheetDeclarations);
                            }
                        }
                        else
                        {
                            var curOutFile = outFile.Replace(".json", $"_{sheetIndex}.json");
                            Logger.Info($"Parsing worksheet {sheetIndex} into file {curOutFile}");
                            WriteOutputJson(inputFile, BuildDeclarations(adapter, inputFile), curOutFile);
                        }
                    }
                    catch (ColumnDetectorException)
                    {
                        Logger.Info($"Skipping empty sheet {sheetIndex} (No headers found exception thrown)");
                    }
                    if (allDeclarations != null)
                    {
                        WriteOutputJson(inputFile, allDeclarations, outFile);
                    }
                }
            }
            else
            {
                WriteOutputJson(inputFile, BuildDeclarations(adapter, inputFile), outFile);
            }

            return 0;
        }
        public static void Main(string[] args)
        {
            var rootCommand = BuildOptions();
            rootCommand.Handler = CommandHandler
                .Create<Options>(options =>
                {
                    Configure(options);
                    Logger.Info($"Command line: {string.Join(" ", args)}");

                    return options.InputFile switch
                    {
                        _ when Directory.Exists(options.InputFile) => ParseDirectory(options.InputFile),
                        _ when options.InputFile.StartsWith("@") || options.InputFile.ContainsAny("*", "?") => ParseByFileMask(options.InputFile),
                        _ => ParseSingleFile(options),
                    };
                });
            rootCommand.Invoke(args);
        }

        private static int ParseSingleFile(Options options)
        {
            try
            {
                Logger.SetOutSecond();
                ParseFile(options.InputFile, options.OutputFile);
            }
            catch (SmartParserException e)
            {
                Logger.Error($"Parsing Exception {e}");
            }
            catch (Exception e)
            {
                Logger.Error($"Unknown Parsing Exception {e}");
                Logger.Info($"Stack: {e.StackTrace}");
            }
            finally
            {
                Logger.SetOutMain();
            }

            if (ColumnPredictor.CalcPrecision)
            {
                Logger.Info(ColumnPredictor.GetPrecisionStr());
            }

            if (Logger.Errors.Count > 0)
            {
                Logger.Info("*** Errors ({0}):", Logger.Errors.Count);

                foreach (var e in Logger.Errors)
                {
                    Logger.Info(e);
                }
            }

            return 0;
        }
    }
}