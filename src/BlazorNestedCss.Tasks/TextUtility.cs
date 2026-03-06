namespace BlazorNestedCss.Tasks;

using System.Text;

internal class TextStringBuilder
{
    StringBuilder _sb = new StringBuilder();

    public void Append(char c) => _sb.Append(c);
    public void Append(string s) => _sb.Append(s);
    public bool HasContent() => _sb.Length > 0;

    public string Flush()
    {
        var result = _sb.ToString();
        _sb.Clear();
        return result;
    }
}

internal class TextStringReader
{
    private readonly string _readerText;
    public int Position { get; set; }
    public string Remaining => _readerText.Substring(Position);

    public TextStringReader(string text) => _readerText = text;
    public char Next() => (char)read();
    public char Peek(int offset = 0) => (char)peek(offset);
    public bool IsEof() => peek() == -1;
    public bool IsEol(char c) => c is '\r' or '\n' || IsEof();

    private int peek(int offset = 0)
    {
        int i = Position + offset;
        return i >= 0 && i < _readerText.Length ? _readerText[i] : -1;
    }

    private int read()
    {
        if (Position >= _readerText.Length) return -1;
        return _readerText[Position++];
    }
}
