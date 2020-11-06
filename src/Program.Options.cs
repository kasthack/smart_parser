namespace Smart.Parser
{
    using TI.Declarator.ParserCommon;

    public static partial class Program
    {
        public class Options
        {
            public string OutputFile { get; set; }

            public string License { get; set; }

            public string Log { get; set; }

            public bool SkipLogging { get; set; }

            public Logger.LogLevel Verbose { get; set; }

            public bool Columnsonly { get; set; }

            public bool Checkjson { get; set; }

            public AdapterFamily Adapter { get; set; } = AdapterFamily.Prod;

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
    }
}