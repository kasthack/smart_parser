using System;
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
using CMDLine;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Reflection;

namespace Smart.Parser
{
    public class SmartParserVersions
    {
        public List<Version> versions = new List<Version>();

        public class Version
        {
            public string id = string.Empty;
            public string info = string.Empty;
        }
    }

    public class Program
    {
        public static string OutFile = string.Empty;
        public static string AdapterFamily = "prod";
        private static bool ColumnsOnly = false;

        private static bool CheckJson = false;
        public static int MaxRowsToProcess = -1;
        public static DeclarationField ColumnToDump = DeclarationField.None;
        public static string TolokaFileName = string.Empty;
        public static string HtmlFileName = string.Empty;
        public static bool SkipRelativeOrphan = false;
        public static bool ValidateByApi = false;
        public static bool IgnoreDirectoryIds = false;
        public static bool BuildTrigrams = false;
        public static int? UserDocumentFileId;

        private static string ParseArgs(string[] args)
        {
            var parser = new CMDLineParser();
            var outputOpt = parser.AddStringParameter("-o", "use file for output", false);
            var licenseOpt = parser.AddStringParameter("-license", string.Empty, false);
            var mainLogOpt = parser.AddStringParameter("-log", string.Empty, false);
            var skipLoggingOpt = parser.AddBoolSwitch("-skip-logging", string.Empty);
            var verboseOpt =
                parser.AddStringParameter("-v", "verbose level: debug, info, error", false);
            var columnsOnlyOpt = parser.AddBoolSwitch("-columnsonly", string.Empty);
            var checkJsonOpt = parser.AddBoolSwitch("-checkjson", string.Empty);
            var adapterOpt = parser.AddStringParameter("-adapter", "can be aspose,npoi, microsoft or prod, by default is aspose", false);
            var maxRowsToProcessOpt =
                parser.AddStringParameter("-max-rows", "max rows to process from the input file", false);
            var dumpColumnOpt = parser.AddStringParameter("-dump-column", "dump column identified by enum DeclarationField and exit", false);
            var dumpHtmlOpt = parser.AddStringParameter("-dump-html", "dump table to html", false);
            var tolokaFileNameOpt =
                parser.AddStringParameter("-toloka", "generate toloka html", false);
            var skipRelativeOrphanOpt = parser.AddBoolSwitch("-skip-relative-orphan", string.Empty);
            var apiValidationOpt =
                parser.AddBoolSwitch("-api-validation", "validate JSON output by API call");
            var buildTrigramsOpt = parser.AddBoolSwitch("-build-trigrams", "build trigrams");
            var checkPredictorOpt =
                parser.AddBoolSwitch("-check-predictor", "calc predictor precision");
            var docFileIdOpt = parser.AddStringParameter("-docfile-id", "document id to initialize document/documentfile_id", false);
            var convertedFileStorageUrlOpt = parser.AddStringParameter(
                "-converted-storage-url",
                "document id to initialize document/documentfile_id for example http://disclosures.ru:8091, the default value is read from env variable DECLARATOR_CONV_URL",
                false);
            var fioOnlyOpt = parser.AddBoolSwitch("-fio-only", string.Empty);
            var useDecimalRawNormalizationOpt = parser.AddBoolSwitch("-decimal-raw-normalization", "print raw floats in Russian traditional format");
            var disclosuresOpt = parser.AddBoolSwitch(
                "-disclosures",
                "use disclosures output format: save sheet id to each each section, do not produce many output files but one");
            var versionOpt = parser.AddBoolSwitch("-version", "print version");
            parser.AddHelpOption();
            try
            {
                // parse the command line
                parser.Parse(args);
            }
            catch (Exception ex)
            {
                // show available options
                Console.Write(parser.HelpMessage());
                Console.WriteLine();
                Console.WriteLine("Error: " + ex.Message);
                throw;
            }

            if (versionOpt.isMatched)
            {
                PrintVersion();
                System.Environment.Exit(0);
            }

            if (licenseOpt.isMatched)
            {
                AsposeLicense.SetLicense(licenseOpt.Value.ToString());
                if (!AsposeLicense.Licensed)
                {
                    throw new SmartParserException("Not valid aspose licence " + licenseOpt.Value.ToString());
                }
            }

            Smart.Parser.Lib.Parser.InitializeSmartParser();
            if (maxRowsToProcessOpt.isMatched)
            {
                MaxRowsToProcess = System.Convert.ToInt32(maxRowsToProcessOpt.Value.ToString());
            }

            if (docFileIdOpt.isMatched)
            {
                UserDocumentFileId = System.Convert.ToInt32(docFileIdOpt.Value.ToString());
            }

            if (disclosuresOpt.isMatched)
            {
                DeclarationSerializer.SmartParserJsonFormat = SmartParserJsonFormatEnum.Disclosures;
            }

            var logFileName = string.Empty;
            if (mainLogOpt.isMatched)
            {
                logFileName = Path.GetFullPath(mainLogOpt.Value.ToString());
            }

            Logger.Setup(logFileName, skipLoggingOpt.isMatched);
            if (outputOpt.isMatched)
            {
                OutFile = outputOpt.Value.ToString();
            }

            var verboseLevel = Logger.LogLevel.Info;
            if (verboseOpt.isMatched)
            {
                switch (verboseOpt.Value.ToString())
                {
                    case "info":
                        verboseLevel = Logger.LogLevel.Info;
                        break;
                    case "error":
                        verboseLevel = Logger.LogLevel.Error;
                        break;
                    case "debug":
                        verboseLevel = Logger.LogLevel.Debug;
                        break;
                    default:
                        {
                            throw new Exception("unknown verbose level " + verboseOpt.Value.ToString());
                        }
                }
            }

            Logger.SetLoggingLevel(verboseLevel);

            SkipRelativeOrphan = skipRelativeOrphanOpt.isMatched;
            ValidateByApi = apiValidationOpt.isMatched;

            if (adapterOpt.isMatched)
            {
                AdapterFamily = adapterOpt.Value.ToString();
                if (AdapterFamily != "aspose" &&
                    AdapterFamily != "npoi" &&
                    AdapterFamily != "microsoft" &&
                    AdapterFamily != "prod")
                {
                    throw new Exception("unknown adapter family " + AdapterFamily);
                }
            }

            if (dumpColumnOpt.isMatched)
            {
                ColumnToDump = (DeclarationField)Enum.Parse(typeof(DeclarationField), dumpColumnOpt.Value.ToString());
            }

            if (dumpHtmlOpt.isMatched)
            {
                HtmlFileName = dumpHtmlOpt.Value.ToString();
            }

            if (convertedFileStorageUrlOpt.isMatched)
            {
                IAdapter.ConvertedFileStorageUrl = convertedFileStorageUrlOpt.Value.ToString();
            }

            if (tolokaFileNameOpt.isMatched)
            {
                TolokaFileName = tolokaFileNameOpt.Value.ToString();
            }

            if (useDecimalRawNormalizationOpt.isMatched)
            {
                Smart.Parser.Lib.Parser.UseDecimalRawNormalization = true;
            }

            ColumnsOnly = columnsOnlyOpt.isMatched;
            ColumnOrdering.SearchForFioColumnOnly = fioOnlyOpt.isMatched;
            CheckJson = checkJsonOpt.isMatched;
            BuildTrigrams = buildTrigramsOpt.isMatched;
            ColumnPredictor.CalcPrecision = checkPredictorOpt.isMatched;
            var freeArgs = parser.RemainingArgs();
            return string.Join(" ", freeArgs).Trim(new char[] { '"' });
        }

        public static void PrintVersion()
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            using var stream = currentAssembly.GetManifestResourceStream("Smart.Parser.Resources.versions.txt");
            using var file = new StreamReader(stream);
            var jsonStr = file.ReadToEnd();
            var versions = JsonConvert.DeserializeObject<SmartParserVersions>(jsonStr);
            Console.WriteLine(versions.versions[versions.versions.Count - 1].id);
        }

        public static string BuildOutFileNameByInput(string inputFile) => Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileName(inputFile) + ".json");

        private static bool IsDirectory(string fileName)
        {
            try
            {
                return (File.GetAttributes(fileName) & FileAttributes.Directory) == FileAttributes.Directory;
            }
            catch
            {
                return false;
            }
        }

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
                Logger.Info("Reading files list from " + fileName);

                files = File.ReadAllLines(fileName).ToArray();
            }
            else
            {
                Logger.Info("Parsing files by mask " + fileMask);

                files = Directory.GetFiles(Path.GetDirectoryName(fileMask), Path.GetFileName(fileMask),
                    SearchOption.AllDirectories);
            }

            Logger.Info("Found {0} files", files.Length);

            return ParseMultipleFiles(files, Path.GetDirectoryName(fileMask));
        }

        public static int ParseMultipleFiles(IEnumerable<string> files, string outputDir)
        {
            var parse_results = new Dictionary<string, List<string>>
            {
                {"ok", new List<string>() },
                {"error", new List<string>() },
                {"too_many_errors", new List<string>() },
                {"exception", new List<string>() },
            };

            foreach (var file in files)
            {
                Logger.Info("Parsing file " + file);
                var caught = false;
                try
                {
                    Logger.SetOutSecond();
                    ParseFile(file, BuildOutFileNameByInput(file));
                }
                catch (SmartParserException e)
                {
                    caught = true;
                    Logger.Error("Parsing Exception " + e.ToString());
                    parse_results["exception"].Add(file);
                }
                catch (Exception e)
                {
                    caught = true;
                    Logger.Error("Parsing Exception " + e.ToString());
                    Logger.Debug("Stack: " + e.StackTrace);
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

                if (!caught && Logger.Errors.Count > 0)
                {
                    Logger.Info("Result: error");
                    parse_results["error"].Add(file);
                }

                if (!caught && Logger.Errors.Count == 0)
                {
                    Logger.Info("Result: OK");
                    parse_results["ok"].Add(file);
                }

                if (Logger.Errors.Count > 0)
                {
                    Logger.Info(" Parsing errors ({0})", Logger.Errors.Count);

                    foreach (var e in Logger.Errors)
                    {
                        Logger.Info(e);
                    }
                }
            }

            Logger.Info("Parsing Results:");

            foreach (var key_value in parse_results)
            {
                Logger.Info("Result: {0} ({1})", key_value.Key, key_value.Value.Count);
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
            var extension = Path.GetExtension(inputFile).ToLower();
            switch (extension)
            {
                case ".htm":
                case ".html":
                    return HtmAdapter.CanProcess(inputFile) ? new HtmAdapter(inputFile) : (IAdapter)new AngleHtmlAdapter(inputFile, MaxRowsToProcess);
                case ".pdf":
                case ".xhtml":
                case ".doc":
                case ".rtf":
                case ".toloka_json":
                case ".docx":
                    return GetCommonAdapter(inputFile);
                case ".xls":
                case ".xlsx":
                    if (AdapterFamily == "aspose" || AdapterFamily == "prod")
                    {
                        if (!AsposeLicense.Licensed && extension == ".xls")
                        {
                            throw new Exception("xls file format is not supported");
                        }

                        if (AsposeLicense.Licensed)
                        {
                            return AsposeExcelAdapter.CreateAdapter(inputFile, MaxRowsToProcess);
                        }
                    }
                    else
                    {
                        return AdapterFamily == "npoi" ? NpoiExcelAdapter.CreateAdapter(inputFile, MaxRowsToProcess) : null;
                    }

                    break;
                default:
                    Logger.Error("Unknown file extension " + extension);
                    return null;
            }

            Logger.Error("Cannot find adapter for " + inputFile);
            return null;
        }

        private static IAdapter GetCommonAdapter(string inputFile)
        {
            if (AdapterFamily != "aspose")
            {
                if (AdapterFamily == "prod")
                {
                    return OpenXmlWordAdapter.CreateAdapter(inputFile, MaxRowsToProcess);
                }
            }
            else if (!AsposeLicense.Licensed)
            {
                throw new Exception("doc and docx file format is not supported");
            }

            return AsposeDocAdapter.CreateAdapter(inputFile);
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
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string BuildInputFileId(IAdapter adapter, string filename) => CalculateMD5(filename) + "_" + adapter.GetWorksheetName();

        public static void SaveRandomPortionToToloka(IAdapter adapter, ColumnOrdering columnOrdering,
            Declaration declaration, string inputFileName)
        {
            if (TolokaFileName?.Length == 0)
            {
                return;
            }

            var fileID = BuildInputFileId(adapter, inputFileName);
            using var file = new StreamWriter(TolokaFileName);
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
            var id = fileID + "_" + dataStart + "_" + dataEnd;
            file.WriteLine(id + "\t\"" + jsonStr + "\"\t\t");
        }

        private static Declaration BuildDeclarations(IAdapter adapter, string inputFile)
        {
            Declaration declaration;
            var inputFileName = Path.GetFileName(inputFile);
            var parser = new Lib.Parser(adapter, !SkipRelativeOrphan);

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

                if (ColumnsOnly)
                {
                    return null;
                }

                if (ColumnToDump != DeclarationField.None)
                {
                    DumpColumn(adapter, columnOrdering, ColumnToDump);
                    return null;
                }

                if (columnOrdering.Title != null)
                {
                    Logger.Info("Declaration Title: {0} ", columnOrdering.Title);
                }

                if (columnOrdering.Year != null)
                {
                    Logger.Info("Declaration Year: {0} ", columnOrdering.Year.Value);
                }

                if (columnOrdering.MinistryName != null)
                {
                    Logger.Info("Declaration Ministry: {0} ", columnOrdering.MinistryName);
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
                      columnOrdering.ContainsField(DeclarationField.DeclaredYearlyIncomeThousands)) && !ColumnOrdering.SearchForFioColumnOnly)
                {
                    Logger.Error("Insufficient fields: No any of Declarant Income fields found.");
                    return null;
                }

                declaration = parser.Parse(columnOrdering, BuildTrigrams, UserDocumentFileId);
                SaveRandomPortionToToloka(adapter, columnOrdering, declaration, inputFile);
            }
            else
            {
                declaration = adapter.CurrentScheme.Parse(parser, UserDocumentFileId);
            }

            return declaration;
        }

        private static string DumpDeclarationsToJson(string inputFile, Declaration declaration)
        {
            string schema_errors = null;
            var output = DeclarationSerializer.Serialize(declaration, ref schema_errors);

            if (!string.IsNullOrEmpty(schema_errors))
            {
                Logger.Error("Json schema errors:" + schema_errors);
            }
            else
            {
                Logger.Info("Json schema OK");
            }

            Logger.Info("Output size: " + output.Length);

            if (ValidateByApi)
            {
                var validationResult = ApiClient.ValidateParserOutput(output);
                if (validationResult != "[]")
                {
                    var inputFileName = Path.GetFileName(inputFile);
                    var errorsFileName = "validation_errors_" +
                                            Path.GetFileNameWithoutExtension(inputFileName) + ".json";
                    var rep = MiscSerializer.DeserializeValidationReport(validationResult);
                    File.WriteAllText(errorsFileName, validationResult);
                    Logger.Error("Api validation failed. Errors:" + errorsFileName);
                }
            }

            return output;
        }

        public static void WriteOutputJson(string inputFile, Declaration declarations, string outFile)
        {
            if (declarations != null)
            {
                var output = DumpDeclarationsToJson(inputFile, declarations);
                Logger.Info("Writing json to " + outFile);
                File.WriteAllBytes(outFile, Encoding.UTF8.GetBytes(output));
            }
        }

        public static int ParseFile(string inputFile, string outFile)
        {
            if (CheckJson && File.Exists(outFile))
            {
                Logger.Info("JSON file {0} already exist", outFile);
                return 0;
            }

            if (!File.Exists(inputFile))
            {
                Logger.Info("ERROR: {0} file NOT exists", inputFile);
                return 0;
            }

            ColumnPredictor.InitializeIfNotAlready();

            var logFile = Path.Combine(Path.GetDirectoryName(inputFile),
                Path.GetFileName(inputFile) + ".log");
            Logger.SetSecondLogFileName(Path.GetFullPath(logFile));

            Logger.Info($"Parsing {inputFile}");
            var adapter = GetAdapter(inputFile);

            Logger.Info($"TablesCount = {adapter.GetTablesCount()}");
            Logger.Info($"RowsCount = {adapter.GetRowsCount()}");

            if (adapter.GetTablesCount() == 0 && !inputFile.EndsWith(".toloka_json"))
            {
                throw new SmartParserException("No tables found in document");
            }

            if (HtmlFileName != string.Empty)
            {
                adapter.WriteHtmlFile(HtmlFileName);
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
                            var curOutFile = outFile.Replace(".json", "_" + sheetIndex.ToString() + ".json");
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

        public static int Main(string[] args)
        {
            var inputFile = ParseArgs(args);
            Logger.Info("Command line: " + string.Join(" ", args));
            if (string.IsNullOrEmpty(inputFile))
            {
                Console.WriteLine("no input file or directory");
                return 1;
            }

            if (IsDirectory(inputFile))
            {
                return ParseDirectory(inputFile);
            }

            if (inputFile.Contains("*") || inputFile.Contains("?") || inputFile.StartsWith("@"))
            {
                return ParseByFileMask(inputFile);
            }

            try
            {
                Logger.SetOutSecond();
                if (OutFile?.Length == 0)
                {
                    OutFile = BuildOutFileNameByInput(inputFile);
                }

                ParseFile(inputFile, OutFile);
            }
            catch (SmartParserException e)
            {
                Logger.Error("Parsing Exception " + e.ToString());
            }
            catch (Exception e)
            {
                Logger.Error("Unknown Parsing Exception " + e.ToString());
                Logger.Info("Stack: " + e.StackTrace);
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