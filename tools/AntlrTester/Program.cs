using System;
using CMDLine;
using SmartAntlr;
using System.Diagnostics;

namespace AntlrTester
{
    internal class Program
    {
        private static string ParseType = "realty_all";
        private static string ParseArgs(string[] args)
        {
            var parser = new CMDLineParser();
            var typeOpt = parser.AddStringParameter("--type", "can bet realty_all, country, default is realty_all", false);
            try
            {
                //parse the command line
                parser.Parse(args);
            }
            catch (Exception ex)
            {
                //show available options      
                Console.Write(parser.HelpMessage());
                Console.WriteLine();
                Console.WriteLine("Error: " + ex.Message);
                throw;
            }
            if (typeOpt.isMatched)
            {
                ParseType = typeOpt.Value.ToString();
            }
            var freeArgs = parser.RemainingArgs();
            return string.Join(" ", freeArgs).Trim(new char[] { '"' });
        }

        private static void Main(string[] args)
        {
            var input = ParseArgs(args);
            var output = input + ".result";
            var texts = AntlrCommon.ReadTestCases(input);
            GeneralAntlrParserWrapper parser = null;
            Console.Error.Write(string.Format("Grammar {0}\n", ParseType));
            if (ParseType == "realty_all")
            {
                parser = new AntlrStrictParser();
            }
            else if (ParseType == "country"  )
            {
                parser = new AntlrCountryListParser();
            }
            else
            {
                Debug.Assert(false);
            }
            parser.BeVerbose();
            AntlrCommon.WriteTestCaseResultsToFile(parser, texts, output);
        }
    }
}
