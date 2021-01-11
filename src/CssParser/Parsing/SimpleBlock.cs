using System.Diagnostics;

namespace Leeax.Parsing.CSS
{
    [DebuggerDisplay("Count = {Value.Count}")]
    public class SimpleBlock : IValue
    {
        private readonly char _prefix;
        private readonly char _suffix;

        public SimpleBlock(char prefix, char suffix)
        {
            _prefix = prefix;
            _suffix = suffix;

            Value = new ValueCollection();
        }

        public override string ToString()
        {
            return _prefix + Value.ToString() + _suffix;
        }

        public string ToString(bool trimWhitesapce)
        {
            return _prefix + Value.ToString(trimWhitesapce) + _suffix;
        }

        // TODO: Handle whitespace triming at start/end and after/before ":" and ";"
        public ValueCollection Value { get; }

        object IValue.Value => this.Value;
    }
}