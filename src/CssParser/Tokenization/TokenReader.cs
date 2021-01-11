using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Leeax.Parsing.CSS
{
    // https://www.w3.org/TR/css-syntax-3/#consume-token
    public class TokenReader : IDisposable
    {
        private readonly CodePointReader _reader;

        /// <param name="value">The string to read from. An underlying <see cref="MemoryStream"/> with <see cref="Encoding.UTF8"/> will be used.</param>
        public TokenReader(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _reader = new CodePointReader(
                new MemoryStream(Encoding.UTF8.GetBytes(value)));
        }

        /// <param name="stream">The stream to read from.</param>
        public TokenReader(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _reader = new CodePointReader(stream);
        }

        /// <summary>
        /// Moves to the next <see cref="IToken"/>.
        /// The token is set to the <see cref="CurrentToken"/> property.
        /// </summary>
        /// <returns>Returns <see langword="true"/>, except if the end of the stream was reached.</returns>
        public bool ReadNext()
        {
            ConsumeComment();

            // Read until EOF is reached
            if (!_reader.Read(out int codePoint))
            {
                // TODO: Does the "EOFToken" match the specification?
                CurrentToken = new EOFToken();

                return false;
            }

            if (HandleCodePoint(codePoint))
            {
                return true;
            }

            if (IsDigit((char)codePoint))
            {
                // Reconsume code point
                _reader.MoveBackward(1);

                ConsumeNumeric();
            }
            else if (CheckNameStartCodePoint((char)codePoint))
            {
                // Reconsume code point
                _reader.MoveBackward(1);

                ConsumeIdentLikeToken();
            }
            else
            {
                CurrentToken = new CharToken(TokenType.Delim, (char)codePoint);
            }

            return true;
        }

        public IEnumerable<IToken> ReadAll()
        {
            while (ReadNext())
            {
                yield return CurrentToken;
            }
        }

        private bool HandleCodePoint(int codePoint)
        {
            switch ((char)codePoint)
            {
                case Constants.WHITESPACE:
                case Constants.LINE_FEED:
                case Constants.TAB:
                    CurrentToken = new CharToken(TokenType.Whitespace, ' ');
                    ConsumeWhitespace();
                    break;
                case Constants.QUOTATION_MARK:
                    ConsumeString(Constants.QUOTATION_MARK);
                    break;
                case Constants.NUMBER_SIGN:

                    // Peek next code point
                    if (_reader.Peek(out char secondCodePoint))
                    {
                        // Check if next code point is a "namecodepoint" or the next two code points are a "valid escape"
                        if (CheckNameCodePoint(secondCodePoint)
                            || CheckForValidEscape())
                        {
                            var type = CheckIfThreeCodePointsStartAnIdentifier()
                                ? "id" // If the next 3 code points would start an identifier set the type flag to "id"
                                : "unrestricted";

                            // Return a "hash-token"
                            CurrentToken = new HashToken(ConsumeName(), type);
                            break;
                        }
                    }

                    // Return a "delim-token" with its value set to the current code point
                    CurrentToken = new CharToken(TokenType.Delim, (char)codePoint);

                    break;
                case Constants.APOSTROPHE:
                    ConsumeString(Constants.APOSTROPHE);
                    break;
                case Constants.LEFT_PARENTHESIS:
                    CurrentToken = new CharToken(TokenType.LeftParenthesis, '(');
                    break;
                case Constants.RIGHT_PARENTHESIS:
                    CurrentToken = new CharToken(TokenType.RightParenthesis, ')');
                    break;
                case Constants.COMMA:
                    CurrentToken = new CharToken(TokenType.Comma, ',');
                    break;
                case Constants.FULL_STOP:
                case Constants.PLUS_SIGN:
                case Constants.HYPHEN_MINUS:

                    if (CheckIfThreeCodePointsStartANumber())
                    {
                        // If so, reconsume the current code point ...
                        _reader.MoveBackward(1);

                        // ... and consume a "numeric-token"
                        ConsumeNumeric();
                        break;
                    }

                    // Additional cases for "-"
                    if (codePoint == Constants.HYPHEN_MINUS)
                    {
                        // TODO: Implement the following case ...
                        /*
                         * If the next 2 input code points are U+002D HYPHEN-MINUS U+003E 
                         * GREATER-THAN SIGN (->), consume them and return a <CDC-token>.
                         */

                        // Check this only if code point is a "HYPHEN_MINUS"
                        // -> "PLUS_SIGN" + "FULL_STOP" not affected
                        if (CheckIfThreeCodePointsStartAnIdentifier())
                        {
                            // If so, reconsume the current code point ...
                            _reader.MoveBackward(1);

                            // ... and consume a "ident-like-token"
                            ConsumeIdentLikeToken();
                            break;
                        }
                    }

                    // Return a "delim-token" with its value set to the current code point
                    CurrentToken = new CharToken(TokenType.Delim, (char)codePoint);

                    break;
                case Constants.COLON:
                    CurrentToken = new CharToken(TokenType.Colon, ':');
                    break;
                case Constants.SEMICOLON:
                    CurrentToken = new CharToken(TokenType.Semicolon, ';');
                    break;
                case Constants.LESS_THAN_SIGN:

                    // TODO: Implement the following case ...
                    /* 
                     * If the next 3 input code points are U+0021 EXCLAMATION MARK U+002D 
                     * HYPHEN-MINUS U+002D HYPHEN-MINUS (!--), consume them and return a <CDO-token>.
                     */

                    // Return a "delim-token" with its value set to the current code point
                    CurrentToken = new CharToken(TokenType.Delim, (char)codePoint);

                    break;
                case Constants.COMMERCIAL_AT:

                    if (CheckIfThreeCodePointsStartAnIdentifier())
                    {
                        CurrentToken = new StringToken(TokenType.AtKeyword, ConsumeName());
                        break;
                    }

                    // Return a "delim-token" with its value set to the current code point
                    CurrentToken = new CharToken(TokenType.Delim, (char)codePoint);

                    break;
                case Constants.LEFT_SQUARE_BRACKET:
                    CurrentToken = new CharToken(TokenType.LeftSquareBracket, '[');
                    break;
                case Constants.REVERSE_SOLIDUS:

                    // Reconsume code point to check for a "valid escape"
                    _reader.MoveBackward(1);

                    if (CheckForValidEscape())
                    {
                        ConsumeIdentLikeToken();
                        break;
                    }

                    // This is a parse error
                    CurrentToken = new CharToken(TokenType.Delim, (char)codePoint);

                    // Move forward to initial position
                    _reader.MoveForward(1);

                    break;
                case Constants.RIGHT_SQUARE_BRACKET:
                    CurrentToken = new CharToken(TokenType.RightSquareBracket, ']');
                    break;
                case Constants.LEFT_CURLY_BRACKET:
                    CurrentToken = new CharToken(TokenType.LeftCurlyBracket, '{');
                    break;
                case Constants.RIGHT_CURLY_BRACKET:
                    CurrentToken = new CharToken(TokenType.RightCurlyBracket, '}');
                    break;
                default:
                    return false;
            }

            return true;
        }

        private void ConsumeString(char endingCodePoint)
        {
            var result = string.Empty;

            while (_reader.Read(out int codePoint))
            {
                if (codePoint == endingCodePoint)
                {
                    CurrentToken = new StringToken(TokenType.String, endingCodePoint + result + endingCodePoint);
                    return;
                }

                if (codePoint == Constants.LINE_FEED)
                {
                    // Reconsume current code point
                    _reader.MoveBackward(1);

                    CurrentToken = new StringToken(TokenType.BadString, endingCodePoint + result);
                    return;
                }

                if (codePoint == Constants.REVERSE_SOLIDUS)
                {
                    // If the next code point is EOF, do nothing
                    if (!_reader.Read(out codePoint))
                    {
                        break;
                    }

                    // If the next code point is a newline, consume it
                    if (codePoint == Constants.LINE_FEED)
                    {
                        continue;
                    }

                    // Go back, that the next code point to read is the "\"
                    _reader.MoveBackward(2);

                    // If the stream starts with an valid escape, consume an escaped code point and add it to the string.
                    if (CheckForValidEscape())
                    {
                        result += ConsumeEscapedCodePoint();
                    }
                    else
                    {
                        // Set cursor to the code point after the "\"
                        _reader.MoveForward(1);
                    }

                    continue;
                }

                result += (char)codePoint;
            }

            // EOF = This is a parse error
            CurrentToken = new StringToken(TokenType.String, endingCodePoint + result);
        }

        /// <summary>
        /// Checks if two code points are a valid escape.
        /// Note: This algorithm will not consume any additional code point.
        /// </summary>
        private bool CheckForValidEscape()
        {
            var chars = _reader.PeekMultiple(2);

            return chars.Length == 2 
                && chars[0] == Constants.REVERSE_SOLIDUS
                && chars[1] != Constants.LINE_FEED;
        }

        /// <summary>
        /// Consumes an escaped code point.
        /// Assumes that the U+005C REVERSE SOLIDUS (\) has already been consumed and that the next input code point has already been verified to be part of a valid escape. (see <see cref="CheckForValidEscape"/>)
        /// </summary>
        private char ConsumeEscapedCodePoint()
        {
            if (!_reader.Read(out int codePoint))
            {
                // EOF = This is a parse error, return U+FFFD REPLACEMENT CHARACTER (�).
                return Constants.REPLACEMENT_CHARACTER;
            }

            if (!IsHexDigit((char)codePoint))
            {
                return (char)codePoint;
            }

            string hex = ((char)codePoint).ToString();

            // Consume as many hex digits as possible, but no more than 5.
            // Note that this means 1-6 hex digits have been consumed in total.
            while (hex.Length < 6)
            {
                if (_reader.Read(out codePoint)
                    && IsHexDigit((char)codePoint))
                {
                    hex += codePoint;
                    continue;
                }

                break;
            }

            // If the next input code point is whitespace, consume it as well
            if (!IsWhitespace((char)_reader.Read()))
            {
                // If not, reconsume code point
                _reader.MoveBackward(1);
            }

            var hexParsed = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);

            if (hexParsed == 0 // Number is zero
                || (hexParsed >= Constants.SURROGATE_LOWER && hexParsed <= Constants.SURROGATE_UPPER) // A surrogate is a code point that is in the range U+D800 to U+DFFF, inclusive.
                || hexParsed > Constants.MAX_ALLOWED_CODEPOINT) // Greater than the maximum allowed code point
            {
                return Constants.REPLACEMENT_CHARACTER;
            }

            return (char)hexParsed;
        }

        private bool IsHexDigit(char value)
        {
            return (value >= '0' && value <= '9')
                || (value >= 'a' && value <= 'f')
                || (value >= 'A' && value <= 'F');
        }

        private void ConsumeNumeric()
        {
            var number = ConsumeNumber(out var numberType);

            if (CheckIfThreeCodePointsStartAnIdentifier())
            {
                CurrentToken = new StringToken(TokenType.Dimension, number.ToString(CultureInfo.InvariantCulture) + ConsumeName());
                return;
            }

            if (_reader.Read(out int codePoint))
            {
                if (codePoint == Constants.PERCENTAGE_SIGN)
                {
                    CurrentToken = new NumberToken(TokenType.Percentage, number, numberType);
                    return;
                }

                // Reconsume the code point
                _reader.MoveBackward(1);
            }

            CurrentToken = new NumberToken(TokenType.Number, number, numberType);
        }

        private double ConsumeNumber()
        {
            return ConsumeNumber(out _);
        }

        /// <summary>
        /// Note: This algorithm will not consume any additional code points.
        /// </summary>
        private bool CheckIfThreeCodePointsStartANumber()
        {
            var codePoints = _reader.PeekMultiple(3);

            if (codePoints.Length >= 2)
            {
                if (codePoints[0] == Constants.PLUS_SIGN
                    || codePoints[0] == Constants.HYPHEN_MINUS)
                {
                    if (IsDigit((char)codePoints[1]))
                    {
                        return true;
                    }

                    if (codePoints.Length == 3
                        && codePoints[1] == Constants.FULL_STOP
                        && IsDigit((char)codePoints[2]))
                    {
                        return true;
                    }

                    return false;
                }

                if (codePoints[0] == Constants.FULL_STOP)
                {
                    return IsDigit((char)codePoints[1]);
                }
            }

            if (codePoints.Length > 0)
            {
                return IsDigit((char)codePoints[0]);
            }

            return false;
        }

        /// <summary>
        /// Note: This algorithm will not consume any additional code points.
        /// </summary>
        private bool CheckIfThreeCodePointsStartAnIdentifier()
        {
            if (_reader.Read(out int codePoint)) // First code point
            {
                if (codePoint == Constants.HYPHEN_MINUS
                    && (_reader.Peek(out int secondCodePoint)
                        && (secondCodePoint == Constants.HYPHEN_MINUS
                            || CheckNameStartCodePoint((char)secondCodePoint)
                            || CheckForValidEscape())))
                {
                    // Reconsume first code point
                    _reader.MoveBackward(1);

                    return true;
                }

                // Reconsume first code point
                _reader.MoveBackward(1);

                if (CheckNameStartCodePoint((char)codePoint))
                {
                    return true;
                }
            }

            return CheckForValidEscape();
        }

        /// <summary>
        /// Consumes a number from the stream.
        /// Note: This algorithm does not do the verification of the first few code points that are necessary to ensure a number can be obtained from the stream.
        /// </summary>
        private double ConsumeNumber(out string type)
        {
            type = "integer";

            var result = string.Empty;
            var codePoint = _reader.Read();

            if (codePoint == Constants.PLUS_SIGN
                || codePoint == Constants.HYPHEN_MINUS)
            {
                result += (char)codePoint;

                codePoint = _reader.Read();
            }

            // While the next input code point is a digit, consume it and append it to result (repr)
            while (IsDigit((char)codePoint))
            {
                result += (char)codePoint;

                codePoint = _reader.Read();
            }

            // Check if the following two input code points are "." followed by a digit
            if (codePoint == Constants.FULL_STOP)
            {
                codePoint = _reader.Read();
                
                if (IsDigit((char)codePoint))
                {
                    // Set type to "number"
                    type = "number";

                    if (result == string.Empty)
                    {
                        result = "0";
                    }

                    // Append them to result (repr)
                    result += "." + (char)codePoint;

                    codePoint = _reader.Read();

                    // While the next input code point is a digit, consume it and append it to result (repr)
                    while (IsDigit((char)codePoint))
                    {
                        result += (char)codePoint;

                        codePoint = _reader.Read();
                    }
                }
            }

            // Reconsume the last code point
            _reader.MoveBackward(1);

            // TODO: Add support for <number>E+5
            // Rules:
            // If the next 2 or 3 input code points are U+0045 LATIN CAPITAL LETTER E (E) or U+0065 LATIN SMALL LETTER E (e), optionally followed by U+002D HYPHEN-MINUS (-) or U+002B PLUS SIGN (+), followed by a digit, then:
            // Consume them.
            // Append them to repr.
            // Set type to "number".
            // While the next input code point is a digit, consume it and append it to result (repr)

            // TODO: Validate if default implementation handle parsing correctly.
            // For more info see the documentation: "4.3.13. Convert a string to a number"
            return double.Parse(result, CultureInfo.InvariantCulture);
        }

        private void ConsumeComment()
        {
            if (_reader.Read(out int codePoint)
                && codePoint != Constants.SOLIDUS)
            {
                _reader.MoveBackward(1);
                return;
            }

            if (_reader.Read(out codePoint)
                && codePoint != Constants.ASTERISK)
            {
                _reader.MoveBackward(2);
                return;
            }

            // Consume until EOF or ...
            while (_reader.Read(out codePoint))
            {
                // ... '*/' code point
                if (codePoint == Constants.ASTERISK
                     && _reader.Peek() == Constants.SOLIDUS)
                {
                    _reader.MoveForward(1);
                    return;
                }
            }
        }

        private void ConsumeIdentLikeToken()
        {
            var name = ConsumeName();
            var isNextLeftParenthesis = _reader.Read() == Constants.LEFT_PARENTHESIS;

            if (isNextLeftParenthesis
                && name.Length == 3
                && (name[0] == 'U' || name[0] == 'u')
                && (name[1] == 'R' || name[1] == 'r')
                && (name[2] == 'L' || name[2] == 'l'))
            {
                var currentCodePoint = _reader.Read();

                if (IsWhitespace((char)currentCodePoint))
                {
                    while (_reader.Read(out currentCodePoint) 
                        && IsWhitespace((char)currentCodePoint))
                    {
                        // No action required, we only want to consume as much whitespace as possible
                    }
                }

                // Reconsume code point
                _reader.MoveBackward(1);

                if ((char)currentCodePoint == Constants.QUOTATION_MARK
                    || (char)currentCodePoint == Constants.APOSTROPHE)
                    //|| (char)currentCodePoint == Constants.WHITESPACE)
                {
                    CurrentToken = new StringToken(TokenType.Function, name);
                    return;
                }
                    
                ConsumeUrl();
                return;
            }
            else if (isNextLeftParenthesis)
            {
                CurrentToken = new StringToken(TokenType.Function, name);
                return;
            }

            // Reconsume code point
            _reader.MoveBackward(1);

            CurrentToken = new StringToken(TokenType.Ident, name);
        }

        /// <summary>
        /// Note: This algorithm assumes that the initial "url(" has already been consumed. 
        /// This algorithm also assumes that it’s being called to consume an "unquoted" value, like url(foo). 
        /// A quoted value, like url("foo"), is parsed as a "function-token". 
        /// <see cref="ConsumeIdentLikeToken"/> automatically handles this distinction; this algorithm shouldn’t be called directly otherwise.
        /// </summary>
        private void ConsumeUrl()
        {
            var result = string.Empty;

            ConsumeWhitespace();

            while (_reader.Read(out int codePoint))
            {
                if (codePoint == Constants.RIGHT_PARENTHESIS)
                {
                    break;
                }

                if (ConsumeWhitespace())
                {
                    // If EOF is reached or next code point is ")"
                    if (!_reader.Peek(out int nextCodePoint)
                        || nextCodePoint == Constants.RIGHT_PARENTHESIS)
                    {
                        break;
                    }

                    // No whitespace in url allowed
                    ConsumeBadUrlRemnants();
                    CurrentToken = new StringToken(TokenType.BadUrl, result);
                    return;
                }

                switch (codePoint)
                {
                    case Constants.QUOTATION_MARK:
                    case Constants.APOSTROPHE:
                    case Constants.LEFT_PARENTHESIS:
                        ConsumeBadUrlRemnants();
                        CurrentToken = new StringToken(TokenType.BadUrl, result);
                        return;
                }

                if (codePoint == Constants.REVERSE_SOLIDUS)
                {
                    _reader.MoveBackward(1);

                    if (CheckForValidEscape())
                    {
                        result += ConsumeEscapedCodePoint();
                    }
                    else
                    {
                        _reader.MoveForward(1);

                        ConsumeBadUrlRemnants();
                        CurrentToken = new StringToken(TokenType.BadUrl, result);
                    }
                }

                result += (char)codePoint;
            }

            CurrentToken = new StringToken(TokenType.Url, result);
        }

        private void ConsumeBadUrlRemnants()
        {
            while (_reader.Read(out int codePoint))
            {
                if (codePoint == Constants.RIGHT_PARENTHESIS)
                {
                    return;
                }

                if (CheckForValidEscape())
                {
                    ConsumeEscapedCodePoint();
                }
            }
        }

        /// <summary>
        /// Returns a string containing the largest name that can be formed from adjacent code points in the stream, starting from the first.
        /// </summary>
        private string ConsumeName()
        {
            var result = string.Empty;

            while (_reader.Read(out char codePoint))
            {
                if (CheckNameCodePoint(codePoint))
                {
                    result += codePoint;
                    continue;
                }

                // Reconsume code point
                _reader.MoveBackward(1);

                if (CheckForValidEscape())
                {
                    result += ConsumeEscapedCodePoint();
                    continue;
                }

                break;
            }

            return result;
        }

        private bool CheckNameCodePoint(char value)
        {
            return CheckNameStartCodePoint(value)
                || IsDigit(value)
                || value == Constants.HYPHEN_MINUS;
        }

        // A letter, a non-ASCII code point, or U+005F LOW LINE(_).
        private bool CheckNameStartCodePoint(char value)
        {
            return IsLetter(value)
                || value >= Constants.CONTROL
                || value == Constants.UNDERSCORE;
        }

        // A-Z & a-z
        private bool IsLetter(char value)
        {
            return char.IsLetter(value);
        }

        // A code point between U+0030 DIGIT ZERO (0) and U+0039 DIGIT NINE (9) inclusive.
        private bool IsDigit(char value)
        {
            return value >= 0x0030
                && value <= 0x0039;
        }

        // A newline, U+0009 CHARACTER TABULATION, or U+0020 SPACE.
        private bool IsWhitespace(char value)
        {
            return value == Constants.WHITESPACE
                || value == Constants.LINE_FEED
                || value == Constants.TAB;
        }

        private bool ConsumeWhitespace()
        {
            var consumedAny = false;

            while (_reader.Peek(out char codePoint)
                && IsWhitespace(codePoint))
            {
                _reader.MoveForward(1);

                consumedAny = true;
            }

            return consumedAny;
        }

        /// <summary>
        /// Releases all resources used by the underlying <see cref="Stream"/>.
        /// </summary>
        public void Dispose()
        {
            _reader?.Dispose();
        }

        /// <summary>
        /// The last read <see cref="IToken"/>.
        /// To move to the next token, call <see cref="ReadNext"/>.
        /// <para />
        /// The <see cref="EOFToken"/> (<see cref="TokenType.EOF"/>) indicated that the <see cref="TokenReader"/> reached its end.
        /// </summary>
        public IToken CurrentToken { get; private set; }
    }
}