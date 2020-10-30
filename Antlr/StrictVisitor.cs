using System;
using System.Collections.Generic;
using TI.Declarator.ParserCommon;
using Antlr4.Runtime;

namespace SmartAntlr
{
    public class StrictVisitor : StrictBaseVisitor<object>
    {
        public List<GeneralParserPhrase> Lines = new List<GeneralParserPhrase>();
        public GeneralAntlrParserWrapper ParserWrapper;

        public StrictVisitor(GeneralAntlrParserWrapper parser) => this.ParserWrapper = parser;
        private RealtyFromText InitializeOneRecord(Strict.RealtyContext context)
        {
            var record = new RealtyFromText(this.ParserWrapper, context);
            if (context.own_type() != null)
            {
                record.OwnType = context.own_type().OWN_TYPE().GetText();
            }
            if (context.realty_type() != null)
            {
                record.RealtyType = context.realty_type().REALTY_TYPE().GetText();
            }

            if (context.square()?.square_value() != null)
            {
                var sc = context.square();
                record.InitializeSquare(sc.square_value().GetText(), sc.HECTARE() != null);
            }
            if (context.own_type()?.realty_share() != null)
            {
                record.RealtyShare = context.own_type().realty_share().GetText();
            }
            if (context.realty_share() != null)
            {
                record.RealtyShare = context.realty_share().GetText();
            }
            if (context.COUNTRY() != null)
            {
                record.Country = this.ParserWrapper.GetSourceTextByTerminalNode(context.COUNTRY());
            }
            return record;
        }

        public override object VisitRealty(Strict.RealtyContext context)
        {
            var line = this.InitializeOneRecord(context);
            this.Lines.Add(line);
            return line;
        }
        public override object VisitSquareAndCountry(Strict.SquareAndCountryContext context)
        {
            if (this.ParserWrapper.Parser.NumberOfSyntaxErrors > 0)
            {
                return null;
            }
            var debug = context.ToStringTree(this.ParserWrapper.Parser);
            var record = new RealtyFromText(this.ParserWrapper, context);
            if (context.square()?.square_value() != null)
            {
                var sc = context.square();
                record.InitializeSquare(sc.square_value().GetText(), sc.HECTARE() != null);
            }
            if (context.square_value() != null)
            {
                record.InitializeSquare(context.square_value().GetText(), false);
            }
            if (context.COUNTRY() != null)
            {
                record.Country = this.ParserWrapper.GetSourceTextByTerminalNode(context.COUNTRY());
            }

            this.Lines.Add(record);
            return record;
        }
    }

    public class AntlrStrictParser : GeneralAntlrParserWrapper
    {
        public enum StartFromRootEnum
        {
            realty_list,
            square_and_country
        }

        public StartFromRootEnum StartFromRoot;
        public AntlrStrictParser(StartFromRootEnum startFromRoot = StartFromRootEnum.realty_list) => this.StartFromRoot = startFromRoot;
        public override List<GeneralParserPhrase> Parse(string inputText)
        {
            try {
                this.InitLexer(inputText);
                var parser = new Strict(this.CommonTokenStream, this.Output, this.ErrorOutput);
                this.Parser = parser;
                var visitor = new StrictVisitor(this);
                Antlr4.Runtime.Tree.IParseTree context = this.StartFromRoot switch
                {
                    StartFromRootEnum.square_and_country => parser.squareAndCountry(),
                    _ => parser.realty_list(),
                };
                if (this.StartFromRoot == StartFromRootEnum.square_and_country)
                {
                    parser.ErrorHandler = new BailErrorStrategy();
                }
                visitor.Visit(context);
                return visitor.Lines;
            }
            catch (Exception e)
            {
                Logger.Error("exception: {0}", e.Message);
                return new List<GeneralParserPhrase>();
            }
        }
    }
}