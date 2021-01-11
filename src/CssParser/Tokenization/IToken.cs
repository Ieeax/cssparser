namespace Leeax.Parsing.CSS
{
    public interface IToken : IValue
    {
        //object Value { get; }

        TokenType TokenType { get; }
    }
}