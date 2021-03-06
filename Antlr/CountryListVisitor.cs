﻿using System.Collections.Generic;

namespace SmartAntlr
{

    public class CountryListVisitor : CountryListBaseVisitor<object>
    {
        public List<GeneralParserPhrase> Lines = new List<GeneralParserPhrase>();
        public GeneralAntlrParserWrapper Parser;

        public CountryListVisitor(GeneralAntlrParserWrapper parser)
        {
            Parser = parser;
        }
        public override object VisitCountry(CountryList.CountryContext context)
        {
            var line = new GeneralParserPhrase(Parser, context);
            Lines.Add(line);
            return line;
        }
    }


    public class AntlrCountryListParser : GeneralAntlrParserWrapper
    {
        public override List<GeneralParserPhrase> Parse(string inputText)
        {
            InitLexer(inputText);
            var parser = new CountryList(CommonTokenStream, Output, ErrorOutput);
            var context = parser.countries();
            var visitor = new CountryListVisitor(this);
            visitor.Visit(context);
            return visitor.Lines;
        }


    }
}