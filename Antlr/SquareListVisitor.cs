using System.Collections.Generic;

using Antlr4.Runtime;

namespace SmartAntlr
{
    public class SquareListVisitor : SquareListBaseVisitor<object>
    {
        public List<GeneralParserPhrase> Lines = new List<GeneralParserPhrase>();
        public GeneralAntlrParserWrapper ParserWrapper;

        public SquareListVisitor(GeneralAntlrParserWrapper parser) => this.ParserWrapper = parser;

        public override object VisitBareScore(SquareList.BareScoreContext context)
        {
            var start = context.Start.StartIndex;
            var end = context.Stop.StopIndex;
            var debug = context.ToStringTree(this.ParserWrapper.Parser);
            var line = new GeneralParserPhrase(this.ParserWrapper, context);
            this.Lines.Add(line);
            return line;
        }
    }

    public class AntlrSquareParser : GeneralAntlrParserWrapper
    {
        public override List<GeneralParserPhrase> Parse(string inputText)
        {
            this.InitLexer(inputText);
            var parser = new SquareList(this.CommonTokenStream, this.Output, this.ErrorOutput);
            this.Parser = parser;
            parser.ErrorHandler = new BailErrorStrategy();
            try
            {
                var context = parser.bareSquares();
                var visitor = new SquareListVisitor(this);
                visitor.Visit(context);
                return visitor.Lines;
            }
            catch (Antlr4.Runtime.Misc.ParseCanceledException)
            {
                return new List<GeneralParserPhrase>();
            }
            catch (RecognitionException)
            {
                return new List<GeneralParserPhrase>();
            }
        }
    }
}