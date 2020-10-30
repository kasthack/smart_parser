namespace TI.Declarator.ParserCommon
{
    public class Vehicle
    {
        public Vehicle(string text, string type = null, string model = null)
        {
            this.Text = text;
            this.Type = type;
            this.Model = model;
        }

        public string Text;
        public string Type;
        public string Model;

        public static implicit operator Vehicle(string v) => new Vehicle(v);

        public static implicit operator string(Vehicle v) => v.Text;
    }
}
