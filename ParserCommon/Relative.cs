﻿namespace TI.Declarator.ParserCommon
{
    public class Relative : Person
    {
        public RelationType RelationType { get; set; }
        public override int? PersonIndex { get; set; }
    }
}
