using System;
using System.Diagnostics;
using System.IO;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Office2Txt
{
    internal class Program
    {
        private static string ProcessDocxPart(OpenXmlPartRootElement part)
        {
            var s = "";
            foreach (var p in part.Descendants<Paragraph>())
            {
                s += p.InnerText + "\n";
            }
            return s;
        }
        private static string ProcessDocx(string inputFile)
        {
            var doc =
                WordprocessingDocument.Open(inputFile, false);
            var s = "";
            foreach (OpenXmlPart h in doc.MainDocumentPart.HeaderParts) {
                s += ProcessDocxPart(h.RootElement);
            }
            s += ProcessDocxPart(doc.MainDocumentPart.Document);
            doc.Close();
            return s;
        }
        private static void Main(string[] args)
        {
            Smart.Parser.Adapters.AsposeLicense.SetAsposeLicenseFromEnvironment();
            Debug.Assert(args.Length == 2);
            var  inputFile = args[0];
            var outFile = args[1];
            var extension = Path.GetExtension(inputFile).ToLower();
            var text = "";
            if (extension == ".docx")
            {
                text = ProcessDocx(inputFile);
            }
            else
            {
                Console.WriteLine("cannot process " + inputFile);
                Environment.Exit(1);
            }
            File.WriteAllText(outFile, text);
        }
    }
}
