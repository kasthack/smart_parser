using System.Xml.Linq;
using System.IO.Compression;
using System.Threading;
using System.IO;
using System;
using System.Linq;
using System.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using Parser.Lib;
using System.Runtime.InteropServices;
using TI.Declarator.ParserCommon;

namespace Smart.Parser.Adapters
{
    public class ConversionServerClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            var w = base.GetWebRequest(uri);
            w.Timeout = 5 * 60 *  1000; // 5 minutes
            return w;
        }
    }
    public static class UriFixer
    {
        public static void FixInvalidUri(Stream fs, Func<string, Uri> invalidUriHandler)
        {
            XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
            using var za = new ZipArchive(fs, ZipArchiveMode.Update);
            foreach (var entry in za.Entries.ToList())
            {
                if (!entry.Name.EndsWith(".rels"))
                {
                    continue;
                }

                var replaceEntry = false;
                XDocument entryXDoc = null;
                using (var entryStream = entry.Open())
                {
                    try
                    {
                        entryXDoc = XDocument.Load(entryStream);
                        if (entryXDoc.Root != null && entryXDoc.Root.Name.Namespace == relNs)
                        {
                            var urisToCheck = entryXDoc
                                .Descendants(relNs + "Relationship")
                                .Where(r => r.Attribute("TargetMode") != null && (string)r.Attribute("TargetMode") == "External");
                            foreach (var rel in urisToCheck)
                            {
                                var target = (string)rel.Attribute("Target");
                                if (target != null)
                                {
                                    try
                                    {
                                        var uri = new Uri(target);
                                    }
                                    catch (UriFormatException)
                                    {
                                        var newUri = invalidUriHandler(target);
                                        rel.Attribute("Target").Value = newUri.ToString();
                                        replaceEntry = true;
                                    }
                                }
                            }
                        }
                    }
                    catch (XmlException)
                    {
                        continue;
                    }
                }
                if (replaceEntry)
                {
                    var fullName = entry.FullName;
                    entry.Delete();
                    var newEntry = za.CreateEntry(fullName);
                    using var writer = new StreamWriter(newEntry.Open());
                    using var xmlWriter = XmlWriter.Create(writer);
                    entryXDoc.WriteTo(xmlWriter);
                }
            }
        }
    }
    public class DocxConverter
    {
        private readonly string DeclaratorConversionServerUrl;
        public DocxConverter(string declaratorConversionServerUrl) => this.DeclaratorConversionServerUrl = declaratorConversionServerUrl;
        private static string ToHex(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length * 2);

            for (var i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString("x2"));
            }

            return result.ToString();
        }

        public string DowloadFromConvertedStorage(string filename)
        {
            using var mySHA256 = SHA256.Create();
            string hashValue;
            using (var fileStream = File.Open(filename, FileMode.Open))
            {
                hashValue = ToHex(mySHA256.ComputeHash(fileStream));
            }
            using var client = new ConversionServerClient();
            var url = this.DeclaratorConversionServerUrl + "?sha256=" + hashValue;
            if (!url.StartsWith("http://"))
            {
                url = "http://" + url;
            }
            var docXPath = Path.GetTempFileName();
            Logger.Debug(string.Format("try to download docx from {0} to {1}", url, docXPath));

            try
            {
                client.DownloadFile(url, docXPath);
                Logger.Debug("WebClient.DownloadFile downloaded file successfully");
                Logger.Debug(string.Format("file {0}, size is {1}", docXPath, new System.IO.FileInfo(docXPath).Length));
            }
            catch (WebException exp)
            {
                if (exp.Status == WebExceptionStatus.Timeout)
                {
                    Logger.Debug("Cannot get docx from conversion server in  5 minutes, retry");
                    client.DownloadFile(url, docXPath);
                }
            }

            return docXPath;
        }

        public string ConvertFile2TempDocX(string filename)
        {
            if (filename.EndsWith("pdf"))
            {
                if (this.DeclaratorConversionServerUrl != "")
                {
                    try
                    {
                        return this.DowloadFromConvertedStorage(filename);
                    }
                    catch (Exception exp)
                    {
                        var t = exp.GetType();
                        Logger.Debug("the file cannot be found in conversion server db, try to process this file in place");
                    }
                }
                else
                {
                    Logger.Error("no url for declarator conversion server specified!");
                }
            }
            var docXPath = filename + ".converted.docx";
            if (filename.EndsWith(".html") || filename.EndsWith(".htm"))
            {
                return this.ConvertWithSoffice(filename);
            }
            var saveCulture = Thread.CurrentThread.CurrentCulture;
            // Aspose.Words cannot work well, see 7007_10.html in regression tests
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            var doc = new Aspose.Words.Document(filename);
            doc.RemoveMacros();
            doc.Save(docXPath, Aspose.Words.SaveFormat.Docx);
            Thread.CurrentThread.CurrentCulture = saveCulture;
            doc = null;
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            return docXPath;
        }
        public string ConvertWithSoffice(string fileName)
        {
            if (fileName.EndsWith("pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new SmartParserException("libre office cannot convert pdf");
            }
            var outFileName = Path.ChangeExtension(fileName, "docx");
            if (File.Exists(outFileName))
            {
                File.Delete(outFileName);
            }
            var prg = @"C:\Program Files\LibreOffice\program\soffice.exe";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                prg = "/usr/bin/soffice";
            }
            var outdir = Path.GetDirectoryName(outFileName);
            var args = string.Format(" --headless --writer   --convert-to \"docx:MS Word 2007 XML\"");
            if (outdir != "")
            {
                args += " --outdir " + outdir;
            }

            args += " " + fileName;
            Logger.Debug(prg + " " + args);
            var p = System.Diagnostics.Process.Start(prg, args);
            p.WaitForExit(3 * 60 * 1000); // 3 minutes
            try { p.Kill(true); } catch (InvalidOperationException) { }
            p.Dispose();
            return !File.Exists(outFileName)
                ? throw new SmartParserException(string.Format("cannot convert  {0} with soffice", fileName))
                : outFileName;
        }
    }
}