namespace Leeax.Parsing.CSS
{
    public interface IRule
    {
        RuleType RuleType { get; }

        string ToString(bool trimWhitespace);
    }
}