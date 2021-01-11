using System.Linq;
using Xunit;

namespace Leeax.Parsing.CSS.Tests
{
    public class CssParserTests
    {
        [Theory]
        [InlineData(".test {}", ".test", 2)]
        [InlineData("#test {}", "#test", 1)]
        [InlineData(".test#test {}", ".test#test", 3)]
        [InlineData("#test.test {}", "#test.test", 3)]
        [InlineData("#test:not(.test) {}", "#test:not(.test)", 3)]
        [InlineData("div > .test:not(#test) {}", "div > .test:not(#test)", 8)]
        [InlineData("div > .test::after {}", "div > .test::after", 9)]
        public void ParseQualifiedRulePreludeTest(string ruleString, string expectedPrelude, int expectedPreludeLength)
        {
            using var tokenizer = new TokenReader(ruleString);

            var parser = new CssParser(tokenizer, new CssParserOptions(WhitespaceHandling.Trim));

            var ruleSet = parser.ParseStylesheet();

            Assert.True(ruleSet.Count() == 1);
            Assert.True(ruleSet.First().RuleType == RuleType.QualifiedRule);

            var rule = ruleSet.First() as Rule;

            Assert.True(rule.Prelude.Count == expectedPreludeLength);
            Assert.True(rule.Prelude.ToString() == expectedPrelude);
        }
    }
}