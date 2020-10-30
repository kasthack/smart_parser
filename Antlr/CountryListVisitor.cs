using System.Collections.Generic;

namespace SmartAntlr
{
    public class CountryListVisitor : CountryListBaseVisitor<object>
    {
        public List<GeneralParserPhrase> Lines = new List<GeneralParserPhrase>();
        public GeneralAntlrParserWrapper Parser;

        public CountryListVisitor(GeneralAntlrParserWrapper parser) => this.Parser = parser;

        public override object VisitCountry(CountryList.CountryContext context)
        {
            var line = new GeneralParserPhrase(this.Parser, context);
            this.Lines.Add(line);
            return line;
        }
    }

    public class AntlrCountryListParser : GeneralAntlrParserWrapper
    {
        public override List<GeneralParserPhrase> Parse(string inputText)
        {
            this.InitLexer(inputText);
            var parser = new CountryList(this.CommonTokenStream, this.Output, this.ErrorOutput);
            var context = parser.countries();
            var visitor = new CountryListVisitor(this);
            visitor.Visit(context);
            return visitor.Lines;
        }
    }
}