using System.Collections.Generic;

using Newtonsoft.Json;

namespace SmartAntlr
{
    public class RealtyTypeAndOwnTypeFromText : GeneralParserPhrase
    {
        public string RealtyType = string.Empty;
        public string OwnType = string.Empty;
        public string RealtyShare = string.Empty;

        public RealtyTypeAndOwnTypeFromText(GeneralAntlrParserWrapper parser, RealtyTypeAndOwnType.RealtyContext context) 
            : base(parser, context)
        {
            if (context.own_type() != null)
            {
                this.OwnType = context.own_type().OWN_TYPE().GetText();
            }

            if (context.realty_type() != null)
            {
                this.RealtyType = context.realty_type().REALTY_TYPE().GetText();
            }

            if (context.own_type()?.realty_share() != null)
            {
                this.RealtyShare = context.own_type().realty_share().GetText();
            }

            if (context.realty_share() != null)
            {
                this.RealtyShare = context.realty_share().GetText();
            }
        }

        public override string GetJsonString()
        {
            var my_jsondata = new Dictionary<string, string>
            {
                { "OwnType", this.OwnType },
                { "RealtyType",  this.RealtyType },
            };
            if (this.RealtyShare != string.Empty)
            {
                my_jsondata["RealtyShare"] = this.RealtyShare;
            }

            return JsonConvert.SerializeObject(my_jsondata, Formatting.Indented);
        }
    }

    public class RealtyTypeAndOwnTypeVisitor : RealtyTypeAndOwnTypeBaseVisitor<object>
    {
        public List<GeneralParserPhrase> Lines = new List<GeneralParserPhrase>();
        public GeneralAntlrParserWrapper Parser;

        public RealtyTypeAndOwnTypeVisitor(GeneralAntlrParserWrapper parser) => this.Parser = parser;

        public override object VisitRealty(RealtyTypeAndOwnType.RealtyContext context)
        {
            var line = new RealtyTypeAndOwnTypeFromText(this.Parser, context);
            this.Lines.Add(line);
            return line;
        }
    }

    public class AntlrRealtyTypeAndOwnTypeParser : GeneralAntlrParserWrapper
    {
        public override List<GeneralParserPhrase> Parse(string inputText)
        {
            this.InitLexer(inputText);
            var parser = new RealtyTypeAndOwnType(this.CommonTokenStream, this.Output, this.ErrorOutput);
            var context = parser.realty_list();
            var visitor = new RealtyTypeAndOwnTypeVisitor(this);
            visitor.Visit(context);
            return visitor.Lines;
        }
    }
}