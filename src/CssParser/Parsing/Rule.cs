using System.Diagnostics;

namespace Leeax.Parsing.CSS
{
    [DebuggerDisplay("RuleType = {RuleType}, Prelude = {Prelude}")]
    public class Rule : IRule
    {
        public Rule(RuleType ruleType)
        {
            Prelude = new ValueCollection();
            RuleType = ruleType;
        }

        public override string ToString()
        {
            return Prelude.ToString() + Value?.ToString();
        }

        public string ToString(bool trimWhitespace)
        {
            return Prelude.ToString(trimWhitespace) + Value?.ToString(trimWhitespace);
        }

        public ValueCollection Prelude { get; }

        public SimpleBlock Value { get; set; }

        public RuleType RuleType { get; }
    }
}