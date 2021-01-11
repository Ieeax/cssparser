using System.Diagnostics;

namespace Leeax.Parsing.CSS
{
    [DebuggerDisplay("Name = {Name}, Arguments = {Value.Count}")]
    public class Function : IValue
    {
        public Function(string name)
        {
            Value = new ValueCollection();
            Name = name;
        }

        public override string ToString()
        {
            return Name + "(" + Value.ToString() + ")";
        }

        public string Name { get; }

        public ValueCollection Value { get; }

        object IValue.Value => this.Value;
    }
}