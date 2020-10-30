using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using Antlr4.Runtime;
using System;
using Newtonsoft.Json;
using TI.Declarator.ParserCommon;
using Antlr4.Runtime.Tree;

public class GeneralParserPhrase
{
    private readonly string TextFromLexer = string.Empty;
    private readonly string SourceText = string.Empty;

    public GeneralParserPhrase(GeneralAntlrParserWrapper parser, ParserRuleContext context)
    {
        this.SourceText = parser.GetSourceTextByParserContext(context);
        this.TextFromLexer = context.GetText();
    }

    public virtual string GetJsonString()
    {
        var my_jsondata = new Dictionary<string, string>
            {
                { "value", this.TextFromLexer },
            };
        return JsonConvert.SerializeObject(my_jsondata, Formatting.Indented);
    }

    public string GetText() => this.TextFromLexer;

    public string GetSourceText() => this.SourceText;
}

public class RealtyFromText : GeneralParserPhrase
{
    public string OwnType = string.Empty;
    public string RealtyType = string.Empty;
    public decimal Square = -1;
    public string RealtyShare = string.Empty;
    public string Country = string.Empty;

    public bool IsEmpty() => this.Square == -1 && this.OwnType.Length == 0 && this.RealtyType.Length == 0 && this.RealtyShare.Length == 0 &&
            this.Country.Length == 0;

    public RealtyFromText(GeneralAntlrParserWrapper parser, ParserRuleContext context) : base(parser, context)
    {
    }

    public void InitializeSquare(string strVal, bool hectare)
    {
        if (strVal != string.Empty)
        {
            this.Square = strVal.ParseDecimalValue();
            if (hectare)
            {
                this.Square *= 10000;
            }

            if (this.Square == 0)
            {
                this.Square = -1;
            }
        }
    }

    public override string GetJsonString()
    {
        var my_jsondata = new Dictionary<string, string>
            {
                { "OwnType", this.OwnType },
                { "RealtyType",  this.RealtyType },
                { "Square", this.Square.ToString() },
            };
        if (this.RealtyShare != string.Empty)
        {
            my_jsondata["RealtyShare"] = this.RealtyShare;
        }

        if (this.Country != string.Empty)
        {
            my_jsondata["Country"] = this.Country;
        }

        return JsonConvert.SerializeObject(my_jsondata, Formatting.Indented);
    }
}

public abstract class GeneralAntlrParserWrapper
{
    public string InputTextCaseSensitive;
    protected CommonTokenStream CommonTokenStream;
    protected TextWriter Output = TextWriter.Null;
    protected TextWriter ErrorOutput = TextWriter.Null;
    public Parser Parser = null;

    public GeneralAntlrParserWrapper(bool silent = true)
    {
        if (!silent)
        {
            this.BeVerbose();
        }
    }

    public void BeVerbose()
    {
        this.Output = Console.Out;
        this.ErrorOutput = Console.Error;
    }

    public virtual Lexer CreateLexer(AntlrInputStream inputStream) => new StrictLexer(inputStream, this.Output, this.ErrorOutput);

    public void InitLexer(string inputText)
    {
        inputText = Regex.Replace(inputText, @"\s+", " ");
        inputText = inputText.Trim();
        this.InputTextCaseSensitive = inputText;
        var inputStream = new AntlrInputStream(this.InputTextCaseSensitive.ToLower());
        var lexer = this.CreateLexer(inputStream);
        this.CommonTokenStream = new CommonTokenStream(lexer);
    }

    public abstract List<GeneralParserPhrase> Parse(string inputText);

    public List<string> ParseToJson(string inputText)
    {
        var result = new List<string>();
        if (inputText != null)
        {
            foreach (var i in this.Parse(inputText))
            {
                result.Add(i.GetJsonString());
            }
        }

        return result;
    }

    public List<string> ParseToStringList(string inputText)
    {
        var result = new List<string>();
        if (inputText != null)
        {
            foreach (var item in this.Parse(inputText))
            {
                if (item.GetText() != string.Empty)
                {
                    result.Add(item.GetText());
                }
            }
        }

        return result;
    }

    public string GetSourceTextByParserContext(ParserRuleContext context)
    {
        var start = context.Start.StartIndex;
        var end = this.InputTextCaseSensitive.Length;
        if (context.Stop != null)
        {
            end = context.Stop.StopIndex + 1;
        }

        return end > start ? this.InputTextCaseSensitive[start..end] : context.GetText();
    }

    public string GetSourceTextByTerminalNode(ITerminalNode node)
    {
        var start = node.Symbol.StartIndex;
        var end = node.Symbol.StopIndex + 1;
        return this.InputTextCaseSensitive[start..end];
    }
}

public class AntlrCommon
{
    public static List<string> ReadTestCases(string inputPath)
    {
        var lines = new List<string>();
        foreach (var line in File.ReadLines(inputPath))
        {
            lines.Add(line + "\n");
        }

        var text = string.Empty;
        var texts = new List<string>();

        for (var i = 0; i < lines.Count; ++i)
        {
            var line = lines[i];
            text += line;
            if (line.Trim().Length == 0 || i + 1 == lines.Count)
            {
                text = Regex.Replace(text, @"\s+", " ");
                text = text.Trim();
                if (text.Length > 0)
                {
                    texts.Add(text);
                }

                text = string.Empty;
            }
        }

        return texts;
    }

    public static void WriteTestCaseResultsToFile(GeneralAntlrParserWrapper parser, List<string> texts, string outputPath)
    {
        using var outputFile = new StreamWriter(outputPath)
        {
            NewLine = "\n",
        };
        foreach (var text in texts)
        {
            outputFile.WriteLine(text);
            foreach (var realtyStr in parser.ParseToJson(text))
            {
                outputFile.WriteLine(realtyStr.Replace("\r", string.Empty));
            }

            outputFile.WriteLine(string.Empty);
        }
    }
}