using System.Diagnostics;

namespace Leeax.Parsing.CSS
{

    [DebuggerDisplay("TokenType = {TokenType}, Value = {Value}")]
    public readonly struct StringToken : IToken
    {
        public StringToken(TokenType tokenType, string value)
        {
            TokenType = tokenType;
            Value = value;
        }

        public override string ToString()
        {
            return TokenType switch
            {
                TokenType.AtKeyword => "@" + Value,
                _ => Value
            };
        }

        public TokenType TokenType { get; }

        public string Value { get; }

        object IValue.Value => this.Value;
    }
}