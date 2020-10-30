using System.Collections.Generic;
using System.Globalization;

namespace TI.Declarator.ParserCommon
{
    public interface DataRowInterface
    {
    }

    public abstract class Person
    {
        private static readonly CultureInfo DefaultCulture = CultureInfo.InvariantCulture;

        public List<RealEstateProperty> RealEstateProperties = new List<RealEstateProperty>();
        public List<Vehicle> Vehicles = new List<Vehicle>();
        public decimal? DeclaredYearlyIncome;
        public string DeclaredYearlyIncomeRaw = "";

        public string DataSources = "";

        public List<DataRowInterface> DateRows = new List<DataRowInterface>();

        public string document_position { set; get; }
        virtual public int? PersonIndex { set; get; } = null;
        public int? sheet_index { set; get; }
    }
}
