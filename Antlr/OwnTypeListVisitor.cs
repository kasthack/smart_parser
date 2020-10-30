using System.Collections.Generic;

namespace SmartAntlr
{
    public class OwnTypeListVisitor : OwnTypeListBaseVisitor<object>
    {
        public List<GeneralParserPhrase> Lines = new List<GeneralParserPhrase>();
        public GeneralAntlrParserWrapper Parser;

        public OwnTypeListVisitor(GeneralAntlrParserWrapper parser) => this.Parser = parser;
        public override object VisitOwn_type(OwnTypeList.Own_typeContext context)
        {
            var item = new GeneralParserPhrase(this.Parser, context);
            this.Lines.Add(item);
            return item;
        }
    }

    public class AntlrOwnTypeParser : GeneralAntlrParserWrapper
    {
        public override List<GeneralParserPhrase> Parse(string inputText)
        {
            this.InitLexer(inputText);
            var parser = new OwnTypeList(this.CommonTokenStream, this.Output, this.ErrorOutput);
            var context = parser.own_type_list();
            var visitor = new OwnTypeListVisitor(this);
            visitor.Visit(context);
            return visitor.Lines;
        }
    }
}