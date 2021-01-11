using System.Diagnostics;
using System.Globalization;

namespace Leeax.Parsing.CSS
{
    [DebuggerDisplay("TokenType = {TokenType}, Value = {Value}, Type = {Type}")]
    public readonly struct NumberToken : IToken
    {
        public NumberToken(TokenType tokenType, double value, string type)
        {
            TokenType = tokenType;
            Value = value;
            Type = type;
        }

        public override string ToString()
        {
            return TokenType == TokenType.Percentage
                ? Value.ToString(CultureInfo.InvariantCulture) + "%"
                : Value.ToString(CultureInfo.InvariantCulture);
        }

        public TokenType TokenType { get; }

        public string Type { get; }

        public double Value { get; }

        object IValue.Value => this.Value;
    }
}