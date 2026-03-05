namespace BlazorNestedCss.Tasks;

using System.Collections.Generic;
using System.Text;

/// <summary>
/// Handles Blazor CSS scoping transformations, including ::deep selector processing
/// </summary>
public class BlazorScopeRewriter
{
    public void Rewrite(CssDocument root, string scope)
    {
        bool applyScope = true;
        int applyScopeLevel = 0;
        int currentLevel = 0;

        foreach (var token in root.Tokens)
        {
            switch (token.Type)
            {
                case TokenType.Selector:
                    var continueApplyScope = AddSelectorScope(token, scope, applyScope);
                    if (continueApplyScope != applyScope)
                    {
                        applyScopeLevel = currentLevel;
                        applyScope = false;
                    }
                    break;
                case TokenType.BlockStart:
                    currentLevel++;
                    break;
                case TokenType.BlockEnd:
                    currentLevel--;
                    if (currentLevel == applyScopeLevel)
                    {
                        applyScope = true;
                    }
                    break;

                case TokenType.Declaration:
                    AddDeclarationScope(token, scope);
                    break;
            }
        }
    }

    private bool AddSelectorScope(Token token, string scope, bool applyScope)
    {
        var pieces = SplitIntoParts(token.Value, ',');
        var result = new List<string>();

        var pauseScoping = !applyScope;
        var continueScoping = applyScope;
        var tokenSelector = pieces.GetEnumerator();
        while (tokenSelector.MoveNext())
        {
            var selector = tokenSelector.Current;
            if (string.IsNullOrWhiteSpace(selector))
            {
            }
            else if (selector.StartsWith("/*"))
            {
            }
            else if (selector.StartsWith("@media"))
            {
                return continueScoping;
            }
            else if (selector.StartsWith("@container"))
            {
                return continueScoping;
            }
            else if (selector.StartsWith("@keyframes"))
            {
                if (continueScoping)
                {
                    result.Add(selector);

                    while (tokenSelector.MoveNext())
                    {
                        var name = tokenSelector.Current;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            name = $"{name}-{scope}";
                        }
                        result.Add(name);
                    }
                    continueScoping = false;
                }

                break;
            }
            else if (selector == "::deep")
            {
                tokenSelector.MoveNext();
                pauseScoping = true;
                continueScoping = false;
                continue;
            }
            else if (selector == ",")
            {
                pauseScoping = !applyScope;
            }
            else if (!pauseScoping)
            {
                selector = AddScope(selector, scope);
            }

            result.Add(selector);
        }

        token.Value = string.Join("", result);

        return continueScoping;
    }

    static List<string> animationTimingFunctions = new(["linear", "ease", "ease-in", "ease-out", "ease-in-out", "step-start"]);
    static List<string> animationFillModes = new(["forwards", "backwards", "both", "none"]);
    static List<string> animationDirections = new List<string>(["normal", "reverse", "alternate", "alternate-reverse"]);
    static List<string> animationPlayStates = new List<string>(["running", "paused"]);
    static List<string> animationPlayCounts = new List<string>(["infinite"]);

    private void AddDeclarationScope(Token token, string scope)
    {
        var pieces = SplitIntoParts(token.Value, ':', ';', ',');

        var piecesBeforeColon = pieces.TakeWhile(p => p != ":");
        var property = piecesBeforeColon.LastOrDefault(p => !(string.IsNullOrWhiteSpace(p) || p.StartsWith("/*")));

        if (property == "animation" || property == "animation-name")
        {
            var values = pieces.Skip(piecesBeforeColon.Count() + 1);
            var newValues = AppendAnimationSuffixToAll(values, $"-{scope}");
            token.Value = string.Join("", piecesBeforeColon) + ":" + string.Join("", newValues);
        }

        IEnumerable<string> AppendAnimationSuffixToAll(IEnumerable<string> pieces, string suffix)
        {
            var result = new List<string>();

            foreach (var piece in pieces)
            {
                if (string.IsNullOrWhiteSpace(piece))
                {
                    result.Add(piece);
                    continue;
                }

                if (piece.StartsWith("/*"))
                {
                    result.Add(piece);
                    continue;
                }

                if (piece is ";" or ",")
                {
                    result.Add(piece);
                    continue;
                }

                if (IsCssFunction(piece))
                {
                    result.Add(piece);
                    continue;
                }

                // Skip durations (1s, 200ms)
                if (char.IsNumber(piece[0]) && piece.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(piece);
                    continue;
                }

                // Skip timing functions
                if (animationTimingFunctions.Contains(piece))
                {
                    result.Add(piece);
                    continue;
                }

                // Skip iteration counts
                if (animationPlayCounts.Contains(piece) || int.TryParse(piece, out _))
                {
                    result.Add(piece);
                    continue;
                }

                // Skip fill modes
                if (animationFillModes.Contains(piece))
                {
                    result.Add(piece);
                    continue;
                }

                // Skip directions
                if (animationDirections.Contains(piece))
                {
                    result.Add(piece);
                    continue;
                }

                // Skip play states
                if (animationPlayStates.Contains(piece))
                {
                    result.Add(piece);
                    continue;
                }

                // If we reach here, this is the animation name
                result.Add(piece + suffix);
            }

            return result;
        }

        bool IsCssFunction(string ident)
        {
            return ident.Contains('(');
        }
    }

    private string AddScope(string selector, string scope, string? pseudoClass = null)
    {
        if (selector.StartsWith("&"))
        {
            return selector;
        }

        var sb = new StringBuilder(selector.Length + 32);
        var reader = new TextStringReader(selector);

        while (!reader.IsEof())
        {
            if (sb.Length == 0 && reader.Peek() == '&')
            {
                sb.Append(reader.Next());
            }
            else if (ExtractBlocks(reader, new CommentBlockParameter(p => sb.Append(p.Result))))
            {
            }
            else if (ExtractBlocks(reader, new SpecialCharacterBlockParameter([' ', '>', '+', '~', ','], p => sb.Append(p.Result))))
            {
            }
            // 2. Pseudo-element ::after
            else if (reader.Peek(0) == ':' && reader.Peek(1) == ':')
            {
                sb.Append(reader.Next());
                sb.Append(reader.Next());

                while (IsIdentChar(reader.Peek()))
                    sb.Append(reader.Next());
            }
            // 3. Pseudo-class :hover, :has(), :is(), :not(), :where()
            else if (reader.Peek() == ':')
            {
                sb.Append(reader.Next());

                // name
                var classNameBuilder = new StringBuilder();
                while (IsIdentChar(reader.Peek()))
                    classNameBuilder.Append(reader.Next());

                sb.Append(classNameBuilder);

                // nested selector list
                if (reader.Peek() == '(')
                {
                    sb.Append(reader.Next());

                    int depth = 1;
                    var inner = new StringBuilder();
                    while (!reader.IsEof() && depth > 0)
                    {
                        if (reader.Peek() == '(')
                            depth++;
                        else if (reader.Peek() == ')')
                            depth--;

                        if (depth > 0)
                        {
                            inner.Append(reader.Next());
                        }

                    }

                    var className = classNameBuilder.ToString();
                    switch (className)
                    {
                        case "nth-child" or "nth-last-child" or "nth-of-type" or "nth-last-of-type":
                            // remove the "An+b of" the selector, since it doesn't select an element directly and can't be scoped
                            var innerSelector = inner.ToString().Trim();
                            var pieces = innerSelector.Split(' ');
                            sb.Append(pieces[0]);
                            if (pieces.Length > 1 && pieces[1] == "of")
                                sb.Append(" of ");
                            inner = new StringBuilder(string.Join(" ", pieces.Skip(2)));
                            sb.Append(AddScope(inner.ToString(), scope, className));
                            break;
                        default:
                            sb.Append(AddScope(inner.ToString(), scope, className));
                            break;
                    }
                    sb.Append(reader.Next());
                }
            }
            // 4. Attribute selector [type="text"]
            else if (reader.Peek() == '[')
            {
                var attr = new StringBuilder();
                while (!reader.IsEof() && reader.Peek() != ']')
                    attr.Append(reader.Next());
                attr.Append(reader.Next());

                sb.Append(attr);
            }
            else
            {
                // 5. Simple selector (.foo, #id, tag, &, etc.)
                var simple = new StringBuilder();

                ExtractBlocks(reader,
                    new CommentBlockParameter(p => sb.Append(p.Result)),
                    new WhitespaceBlockParameter(p => sb.Append(p.Result))
                );

                while (!reader.IsEof() &&
                       !char.IsWhiteSpace(reader.Peek()) &&
                       reader.Peek() != '>' &&
                       reader.Peek() != '+' &&
                       reader.Peek() != '~' &&
                       reader.Peek() != ',' &&
                       reader.Peek() != ':' &&
                       reader.Peek() != '[')
                {
                    simple.Append(reader.Next());
                }

                if (simple.Length > 0)
                {
                    sb.Append(simple);
                    sb.Append('[').Append(scope).Append(']');
                }

                ExtractBlocks(reader,
                    new CommentBlockParameter(p => sb.Append(p.Result)),
                    new WhitespaceBlockParameter(p => sb.Append(p.Result))
                );
            }
        }

        return sb.ToString();

        bool IsIdentChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '\\';
        }
    }

    private List<string> SplitIntoParts(string input, params char[] specialChars)
    {
        var parts = new List<string>();

        if (string.IsNullOrEmpty(input))
            return parts;

        var reader = new TextStringReader(input);

        var specialSet = new HashSet<char>(specialChars);

        while (!reader.IsEof())
        {
            if (ExtractBlocks(reader,
                    new CommentBlockParameter(p => parts.Add(p.Result)),
                    new WhitespaceBlockParameter(p => parts.Add(p.Result))
                ))
            {
            }
            else if (specialSet.Contains(reader.Peek()))
            {
                parts.Add(reader.Next().ToString());
            }
            // Non-whitespace token — including any attached paren groups
            else
            {
                var sb = new StringBuilder();
                while (!reader.IsEof())
                {
                    if (reader.Peek() == '(')
                    {
                        int depth = 0;
                        while (!reader.IsEof())
                        {
                            if (reader.Peek() == '(') depth++;
                            else if (reader.Peek() == ')') depth--;
                            sb.Append(reader.Next());
                            if (depth == 0) break;
                        }
                    }
                    else if (char.IsWhiteSpace(reader.Peek())
                        || specialSet.Contains(reader.Peek())
                        || (reader.Peek(0) == '/' && reader.Peek(1) == '*')
                    )
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(reader.Next());
                    }
                }
                if (sb.Length > 0)
                {
                    parts.Add(sb.ToString());
                }
            }
        }

        parts.RemoveAll(p => string.IsNullOrEmpty(p));

        return parts;
    }

    private abstract class BlockParameter
    {
        public string Result { get; set; }
        public Action<BlockParameter> OnResultAvailable { get; set; }
        public BlockParameter(Action<BlockParameter> onResultAvailable)
        {
            Result = string.Empty;
            OnResultAvailable = onResultAvailable;
        }
    }

    private class CommentBlockParameter : BlockParameter
    {
        public CommentBlockParameter(Action<BlockParameter> onResultAvailable) : base(onResultAvailable) { }
    }

    private class WhitespaceBlockParameter : BlockParameter
    {
        public WhitespaceBlockParameter(Action<BlockParameter> onResultAvailable) : base(onResultAvailable) { }
    }

    private class QuotedStringBlockParameter : BlockParameter
    {
        public QuotedStringBlockParameter(Action<BlockParameter> onResultAvailable) : base(onResultAvailable) { }
    }

    private class SpecialCharacterBlockParameter : BlockParameter
    {
        public char[] SpecialChars { get; }

        public SpecialCharacterBlockParameter(char[] specialChars, Action<BlockParameter> onResultAvailable) : base(onResultAvailable)
        {
            SpecialChars = specialChars;
        }
    }

    private bool ExtractBlocks(TextStringReader reader, params BlockParameter[] parameters)
    {
        var anyProcessed = false;

        do
        {
            anyProcessed = false;
            foreach (var parameter in parameters)
            {
                var sb = new StringBuilder();

                switch (parameter)
                {
                    case CommentBlockParameter commentBlockParameter:
                        if (reader.Peek(0) == '/' && reader.Peek(1) == '*')
                        {
                            sb.Append(reader.Next());
                            sb.Append(reader.Next());

                            while (!(reader.Peek(0) == '*' && reader.Peek(1) == '/'))
                            {
                                sb.Append(reader.Next());
                            }

                            sb.Append(reader.Next());
                            sb.Append(reader.Next());
                        }
                        break;

                    case WhitespaceBlockParameter whitespaceBlockParameter:
                        while (char.IsWhiteSpace(reader.Peek(0)))
                        {
                            sb.Append(reader.Next());
                        }
                        break;

                    case SpecialCharacterBlockParameter specialCharacterBlockParameter:
                        while (specialCharacterBlockParameter.SpecialChars.Contains(reader.Peek(0)))
                        {
                            sb.Append(reader.Next());
                        }
                        break;

                    case QuotedStringBlockParameter quoteBlockParameter:
                        if (reader.Peek(0) == '"')
                        {
                            sb.Append(reader.Next());
                            while (reader.Peek(1) != '"')
                            {
                                sb.Append(reader.Next());
                            }
                            sb.Append(reader.Next());
                        }
                        if (reader.Peek(0) == '\'')
                        {
                            sb.Append(reader.Next());
                            while (reader.Peek(1) != '\'')
                            {
                                sb.Append(reader.Next());
                            }
                            sb.Append(reader.Next());
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (sb.Length > 0)
                {
                    parameter.Result = sb.ToString();
                    parameter.OnResultAvailable?.Invoke(parameter);
                    anyProcessed = true;
                }
            }
        } while (anyProcessed);

        return anyProcessed;
    }
}
