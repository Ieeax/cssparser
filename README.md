# Introduction 
A fast and lightweight CSS tokenizer/parser based on .NET 5 and no dependencies.
Implementation follows (mostly) the W3C specification at https://www.w3.org/TR/css-syntax-3/
> **Attention:** Be aware that the library does not support the full specification and thus some complex rules may not be parsable/parsed correctly.

# Getting Started
The following example shows how to use the library:
```csharp
    using Leeax.Parsing.CSS;

    ...

    // Create a token-reader/tokenizer and pass the stylesheet as a string ...
    using var tokenizer = new TokenReader("...");
 
    // ... or pass a stream
    using var tokenizer = new TokenReader(
      new FileStream("style.css", FileMode.Open, FileAccess.Read));

    // Create the parser and pass the tokenizer
    // Here I also pass some options which define how white-space is treated
    var parser = new CssParser(tokenizer, new CssParserOptions(WhitespaceHandling.Trim));
    
    // Parse the stylesheet and return all rules
    var rules = parser.ParseStylesheet();
```

Note: For converting a parsed rule back to a string you can use the `ToString()` method.

# Issues
When encountering any bugs/problems feel free to create an issue.