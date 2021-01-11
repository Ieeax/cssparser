using System.Diagnostics;

namespace Leeax.Parsing.CSS
{
    [DebuggerDisplay("TokenType = {TokenType}")]
    public struct EOFToken : IToken
    {
        public override string ToString()
        {
            return "EOF";
        }

        public TokenType TokenType => TokenType.EOF;

        object IValue.Value => null;
    }
}