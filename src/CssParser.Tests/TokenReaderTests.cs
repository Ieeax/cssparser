using System.Linq;
using Xunit;

namespace Leeax.Parsing.CSS.Tests
{
    public class TokenReaderTests
    {
        [Theory]
        [InlineData("/*comment*/.class{background-color:#fff;}")]
        [InlineData(".class/*comment*/{background-color:#fff;}")]
        [InlineData(".class{background-color:/*comment*/#fff;}")]
        [InlineData(".class{background-color:#fff;/*comment*/}")]
        [InlineData(".class{background-color:#fff;}/*comment*/")]
        public void ConsumeCommentsTest(string stylesheet)
        {
            using var reader = new TokenReader(stylesheet);

            var tokens = reader.ReadAll().ToArray();

            Assert.True(tokens.Length == 8);
            Assert.True(tokens[0].TokenType == TokenType.Delim && tokens[0].Value.Equals('.'));
            Assert.True(tokens[1].TokenType == TokenType.Ident && tokens[1].Value.Equals("class"));
            Assert.True(tokens[2].TokenType == TokenType.LeftCurlyBracket && tokens[2].Value.Equals('{'));
            Assert.True(tokens[3].TokenType == TokenType.Ident && tokens[3].Value.Equals("background-color"));
            Assert.True(tokens[4].TokenType == TokenType.Colon && tokens[4].Value.Equals(':'));
            Assert.True(tokens[5].TokenType == TokenType.Hash && tokens[5].Value.Equals("fff"));
            Assert.True(tokens[6].TokenType == TokenType.Semicolon && tokens[6].Value.Equals(';'));
            Assert.True(tokens[7].TokenType == TokenType.RightCurlyBracket && tokens[7].Value.Equals('}'));
        }

        [Theory]
        [InlineData("#sample{}", "sample", 0, 3)]
        [InlineData("body{color:#fff;}", "fff", 4, 7)]
        public void ConsumeHashTokenTest(string stylesheet, string expectedValue, int hashPosition, int expectedLength)
        {
            using var reader = new TokenReader(stylesheet);

            var tokens = reader.ReadAll().ToArray();

            Assert.True(tokens.Length == expectedLength);
            Assert.True(tokens[hashPosition] is HashToken hashToken 
                && hashToken.TokenType == TokenType.Hash
                && hashToken.Type == "id" 
                && hashToken.Value == expectedValue);
        }

        [Theory]
        [InlineData(".sample      {       }  ", 7)]
        [InlineData("     .test      {   }  ", 8)]
        [InlineData("body.sample   #sample   {   color    :  rgba(   0, .2   ,2   ,  0   }     ", 28)]
        public void ConsumeAsMuchWhitespaceAsPossibleTest(string stylesheet, int expectedLength)
        {
            using var reader = new TokenReader(stylesheet);

            var tokens = reader.ReadAll().ToArray();

            Assert.True(tokens.Length == expectedLength);

            IToken lastToken = null;
            foreach (var curToken in tokens)
            {
                if (lastToken == null)
                {
                    continue;
                }

                Assert.True(lastToken.TokenType == TokenType.Whitespace
                    && curToken.TokenType == TokenType.Whitespace);

                lastToken = curToken;
            }
        }

        [Fact]
        public void ConsumeUrlTest()
        {
            using var reader = new TokenReader(".test { font-family: url(https://www.w3.org/TR/css-syntax-3); }");

            var tokens = reader.ReadAll().ToArray();

            Assert.Equal(TokenType.Url, tokens[8].TokenType);
            Assert.Equal("https://www.w3.org/TR/css-syntax-3", tokens[8].Value);
        }

        [Theory]
        [InlineData("'https://www.w3.org/TR/css-syntax-3'")]
        [InlineData("\"https://www.w3.org/TR/css-syntax-3\"")]
        public void ConsumeUrlFunctionTest(string url)
        {
            using var reader = new TokenReader(".test { font-family: url(" + url + "); }");

            var tokens = reader.ReadAll().ToArray();

            Assert.Equal(TokenType.Function, tokens[8].TokenType);
            Assert.Equal("url", tokens[8].Value);
            Assert.Equal(TokenType.String, tokens[9].TokenType);
            Assert.Equal(url, tokens[9].Value);
        }
    }
}