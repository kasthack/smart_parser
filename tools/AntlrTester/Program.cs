using System;
using System.Runtime.CompilerServices;
//using CMDLine;
using SmartAntlr;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace AntlrTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<ParseType>("-type", () => ParseType.realty_all),
                new Argument<string>("input"),
            };

            rootCommand.Handler = CommandHandler
                .Create<ParseType, string>((type, input) =>
                {
                    var output = input + ".result";
                    var texts = AntlrCommon.ReadTestCases(input);
                    Console.Error.WriteLine($"Grammar { type}");
                    GeneralAntlrParserWrapper parser = type switch
                    {
                        ParseType.realty_all => new AntlrStrictParser(),
                        ParseType.country => new AntlrCountryListParser(),
                        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid parse type"),
                    };
                    parser.BeVerbose();
                    AntlrCommon.WriteTestCaseResultsToFile(parser, texts, output);
                });

            rootCommand.Invoke(args);
        }
        public enum ParseType
        {
            realty_all,
            country,
        }
    }
}
