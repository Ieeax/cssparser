using System.Diagnostics;

namespace Leeax.Parsing.CSS
{
    [DebuggerDisplay("TokenType = {TokenType}, Value = {Value}, Type = {Type}")]
    public readonly struct HashToken : IToken
    {
        public HashToken(string value, string type)
        {
            Value = value;
            Type = type;
        }

        public override string ToString()
        {
            return "#" + Value;
        }

        public TokenType TokenType => TokenType.Hash;

        public string Type { get; }

        public string Value { get; }

        object IValue.Value => this.Value;
    }
}