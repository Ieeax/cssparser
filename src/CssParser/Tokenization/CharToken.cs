using System.Diagnostics;

namespace Leeax.Parsing.CSS
{
    [DebuggerDisplay("TokenType = {TokenType}, Value = {Value}")]
    public readonly struct CharToken : IToken
    {
        public CharToken(TokenType tokenType, char value)
        {
            TokenType = tokenType;
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public TokenType TokenType { get; }

        public char Value { get; }

        object IValue.Value => this.Value;
    }
}