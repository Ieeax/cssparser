using System;
using System.Collections.Generic;

namespace Leeax.Parsing.CSS
{
    public class CssParser
    {
        private readonly CssParserOptions _options;
        private readonly TokenReader _tokenReader;

        public CssParser(TokenReader tokenReader)
            : this(tokenReader, null)
        {
        }

        public CssParser(TokenReader tokenReader, CssParserOptions options)
        {
            if (tokenReader == null)
            {
                throw new ArgumentNullException(nameof(tokenReader));
            }

            _options = options ?? CssParserOptions.Default;
            _tokenReader = tokenReader;
        }

        public IEnumerable<IRule> ParseStylesheet()
        {
            var result = new List<IRule>();

            while (_tokenReader.ReadNext())
            {
                if (_tokenReader.CurrentToken.TokenType == TokenType.Whitespace)
                {
                    continue;
                }

                if (_tokenReader.CurrentToken.TokenType == TokenType.EOF)
                {
                    break;
                }

                if (_tokenReader.CurrentToken.TokenType == TokenType.CDO
                    || _tokenReader.CurrentToken.TokenType == TokenType.CDC)
                {
                    // TODO: Handle the CDO + CDC token
                    continue;
                }

                if (_tokenReader.CurrentToken.TokenType == TokenType.AtKeyword)
                {
                    result.Add(ConsumeAtRule(true));
                    continue;
                }

                var rule = ConsumeQualifiedRule(true);
                if (rule != null)
                {
                    result.Add(rule);
                }
            }

            return result;
        }

        private IRule ConsumeAtRule(bool reconsumeCurrentToken = false)
        {
            var result = new Rule(RuleType.AtRule);

            while (reconsumeCurrentToken
                || _tokenReader.ReadNext())
            {
                if (_tokenReader.CurrentToken.TokenType == TokenType.Semicolon)
                {
                    break;
                }

                if (_tokenReader.CurrentToken.TokenType == TokenType.LeftCurlyBracket)
                {
                    result.Value = ConsumeSimpleBlock('{', '}');
                    break;
                }

                // Add component-value to prelude (selector)
                result.Prelude.AddValue(ConsumeComponentValue());

                if (reconsumeCurrentToken)
                {
                    reconsumeCurrentToken = false;
                }
            }

            TrimWhitespaceWhenRequired(result.Prelude);

            return result;
        }

        private IRule ConsumeQualifiedRule(bool reconsumeCurrentToken = false)
        {
            var result = new Rule(RuleType.QualifiedRule);

            while (reconsumeCurrentToken
                || _tokenReader.ReadNext())
            {
                if (_tokenReader.CurrentToken.TokenType == TokenType.LeftCurlyBracket)
                {
                    result.Value = ConsumeSimpleBlock('{', '}');
                    break;
                }

                // Add component-value to prelude (selector)
                result.Prelude.AddValue(ConsumeComponentValue());

                if (reconsumeCurrentToken)
                {
                    reconsumeCurrentToken = false;
                }
            }

            TrimWhitespaceWhenRequired(result.Prelude);

            return result;
        }

        private void TrimWhitespaceWhenRequired(ValueCollection collection)
        {
            // Remove trailing whitespace if "WhitespaceHandling" is set to "Trim"
            if (_options.WhitespaceHandling == WhitespaceHandling.Trim
                && collection.Count > 0
                && collection[collection.Count - 1] is CharToken token
                && token.TokenType == TokenType.Whitespace)
            {
                collection.RemoveValue(collection.Count - 1);
            }
        }

        /// <summary>
        /// Note: This algorithm assumes that the current input token has already been 
        /// checked to be an "{-token", "[-token", or "(-token".
        /// </summary>
        private SimpleBlock ConsumeSimpleBlock(char prefix, char suffix)
        {
            var result = new SimpleBlock(prefix, suffix);
            var endingToken = _tokenReader.CurrentToken.TokenType switch
            {
                TokenType.LeftParenthesis => TokenType.RightParenthesis,
                TokenType.LeftSquareBracket => TokenType.RightSquareBracket,
                TokenType.LeftCurlyBracket => TokenType.RightCurlyBracket,
                _ => throw new ApplicationException($"Invalid token '{_tokenReader.CurrentToken.TokenType}' detected.")
            };

            while (_tokenReader.ReadNext())
            {
                if (_tokenReader.CurrentToken.TokenType == endingToken)
                {
                    break;
                }

                result.Value.AddValue(ConsumeComponentValue());
            }

            return result;
        }

        private char GetRightBracket(TokenType tokenType)
        {
            return tokenType switch
            {
                TokenType.LeftParenthesis => ')',
                TokenType.LeftSquareBracket => ']',
                TokenType.LeftCurlyBracket => '}',
                _ => throw new ApplicationException($"Invalid token '{_tokenReader.CurrentToken.TokenType}' detected.")
            };
        }

        private IValue ConsumeComponentValue()
        {
            if (_tokenReader.CurrentToken.TokenType == TokenType.LeftParenthesis
                || _tokenReader.CurrentToken.TokenType == TokenType.LeftSquareBracket
                || _tokenReader.CurrentToken.TokenType == TokenType.LeftCurlyBracket)
            {
                return ConsumeSimpleBlock(
                    (char)_tokenReader.CurrentToken.Value, 
                    GetRightBracket(_tokenReader.CurrentToken.TokenType));
            }

            if (_tokenReader.CurrentToken.TokenType == TokenType.Function)
            {
                return ConsumeFunction();
            }

            return _tokenReader.CurrentToken;
        }

        /// <summary>
        /// Note: This algorithm assumes that the current input token has already been checked to be a "function-token".
        /// </summary>
        private IValue ConsumeFunction()
        {
            var result = new Function(_tokenReader.CurrentToken.Value.ToString());

            while (_tokenReader.ReadNext())
            {
                if (_tokenReader.CurrentToken.TokenType == TokenType.RightParenthesis)
                {
                    break;
                }

                result.Value.AddValue(ConsumeComponentValue());
            }

            return result;
        }
    }
}