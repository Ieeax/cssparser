namespace Leeax.Parsing.CSS
{
    public class CssParserOptions
    {
        public CssParserOptions()
        {
        }

        public CssParserOptions(WhitespaceHandling whitespaceHandling)
        {
            WhitespaceHandling = whitespaceHandling;
        }

        public WhitespaceHandling WhitespaceHandling { get; set; }

        public static CssParserOptions Default => new CssParserOptions();
    }
}