using System.Diagnostics;

namespace BlazorNestedCss.Tasks;

public class CssDocument
{
    public List<Token> Tokens { get; } = new();
}

public enum TokenType
{
    Selector,
    Declaration,
    BlockStart,
    BlockEnd,
    CommentBlock,
    Whitespace
}

[DebuggerDisplay("{Type} => {Value}")]
public class Token
{
    public string Value { get; set; }
    public TokenType Type { get; set; }

    public Token(TokenType type, string token)
    {
        Value = token;
        Type = type;
    }
}
