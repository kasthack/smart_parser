﻿namespace TI.Declarator.ParserCommon{    public class DeclarationProperties    {        public string SheetTitle { get; set; }
        public int? Year { get; set; }
        public int? DocumentFileId { get; set; }
        public string ArchiveFileName { get; set; }
        public int? SheetNumber { get; set; }        public bool IgnoreThousandMultipler = false;    }}