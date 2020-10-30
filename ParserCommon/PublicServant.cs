using System.Collections.Generic;

namespace TI.Declarator.ParserCommon
{
    public class PublicServant : Person
    {
        public string NameRaw { get; set; }

        public string Occupation { get; set; }

        public string Department { get; set; }

        public int? Index { get; set; }

        public void AddRelative(Relative relative)
        {
            relative.PersonIndex = this.relatives.Count + 1;
            this.relatives.Add(relative);
        }

        public IEnumerable<Relative> Relatives => this.relatives;

        private readonly List<Relative> relatives = new List<Relative>();

        public override int? PersonIndex => null;

        public ColumnOrdering Ordering;
    }
}
