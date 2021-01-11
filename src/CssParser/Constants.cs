namespace Leeax.Parsing.CSS
{
    internal class Constants
    {
        public const char WHITESPACE = ' ';
        public const char LINE_FEED = '\n';
        public const char CARRIAGE_RETURN = '\r';
        public const char FORM_FEED = '\f';
        public const char TAB = '\t';
        public const char QUOTATION_MARK = '"';
        public const char NUMBER_SIGN = '#';
        public const char APOSTROPHE = '\'';
        public const char LEFT_PARENTHESIS = '(';
        public const char RIGHT_PARENTHESIS = ')';
        public const char PLUS_SIGN = '+';
        public const char COMMA = ',';
        public const char HYPHEN_MINUS = '-';
        public const char FULL_STOP = '.';
        public const char COLON = ':';
        public const char SEMICOLON = ';';
        public const char LESS_THAN_SIGN = '<';
        public const char COMMERCIAL_AT = '@';
        public const char LEFT_SQUARE_BRACKET = '[';
        public const char REVERSE_SOLIDUS = '\\';
        public const char RIGHT_SQUARE_BRACKET = ']';
        public const char LEFT_CURLY_BRACKET = '{';
        public const char RIGHT_CURLY_BRACKET = '}';
        public const char UNDERSCORE = '_';
        public const char SOLIDUS = '/';
        public const char ASTERISK = '*';
        public const char PERCENTAGE_SIGN = '%';

        public const int NULL = 0x0000;
        public const int CONTROL = 0x0080; // non-ASCII code point -> A code point with a value equal to or greater than U+0080 <control>.
        public const int MAX_ALLOWED_CODEPOINT = 0x10FFFF;
        public const int SURROGATE_LOWER = 0xD800;
        public const int SURROGATE_UPPER = 0xDFFF;
        public const int BOM = 0xFEFF;

        public const char REPLACEMENT_CHARACTER = (char)0xFFFD;

        public const int EOF = -1;
    }
}