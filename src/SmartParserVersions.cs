namespace Smart.Parser
{
    using System.Collections.Generic;

    public class SmartParserVersions
    {
        public List<Version> versions { get; set; } = new List<Version>();

        public class Version
        {
            public string id { get; set; } = string.Empty;

            public string info { get; set; } = string.Empty;
        }
    }
}