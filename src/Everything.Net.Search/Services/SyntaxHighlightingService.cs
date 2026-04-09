using Everything.Net.Search.Models;
using Spectre.Console;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace Everything.Net.Search.Services;

internal sealed class SyntaxHighlightingService
{
    private static readonly Color CommentColor = new(138, 156, 123);
    private static readonly Color StringColor = new(214, 181, 116);
    private static readonly Color NumberColor = new(209, 154, 102);
    private static readonly Color KeywordColor = new(201, 134, 108);
    private static readonly Color TypeColor = new(120, 178, 162);
    private static readonly Color FunctionColor = new(214, 176, 112);
    private static readonly Color OperatorColor = new(184, 176, 208);

    private static readonly Style CommentStyle = new(CommentColor, Color.Default, Decoration.Italic);
    private static readonly Style StringStyle = new(StringColor, Color.Default);
    private static readonly Style NumberStyle = new(NumberColor, Color.Default);
    private static readonly Style KeywordStyle = new(KeywordColor, Color.Default, Decoration.Bold);
    private static readonly Style TypeStyle = new(TypeColor, Color.Default);
    private static readonly Style FunctionStyle = new(FunctionColor, Color.Default);
    private static readonly Style OperatorStyle = new(OperatorColor, Color.Default);

    private readonly RegistryOptions _registryOptions = new(TextMateSharp.Grammars.ThemeName.DarkPlus);
    private readonly Registry _registry;

    public SyntaxHighlightingService()
    {
        _registry = new Registry(_registryOptions);
    }

    public bool TryHighlight(string path, IReadOnlyList<string> lines, out IReadOnlyList<FinderPreviewLine> highlightedLines)
    {
        highlightedLines = Array.Empty<FinderPreviewLine>();

        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var scope = _registryOptions.GetScopeByExtension(extension);
        if (string.IsNullOrWhiteSpace(scope))
        {
            return false;
        }

        IGrammar? grammar;
        try
        {
            grammar = _registry.LoadGrammar(scope);
        }
        catch
        {
            return false;
        }

        if (grammar is null)
        {
            return false;
        }

        var result = new List<FinderPreviewLine>(lines.Count);
        IStateStack? ruleStack = null;

        try
        {
            foreach (var line in lines)
            {
                var tokenizeResult = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
                ruleStack = tokenizeResult.RuleStack;
                result.Add(BuildHighlightedLine(line, tokenizeResult.Tokens));
            }
        }
        catch
        {
            highlightedLines = Array.Empty<FinderPreviewLine>();
            return false;
        }

        highlightedLines = result;
        return true;
    }

    private static FinderPreviewLine BuildHighlightedLine(string line, IReadOnlyList<IToken> tokens)
    {
        if (string.IsNullOrEmpty(line))
        {
            return FinderPreviewLine.Plain(string.Empty);
        }

        var spans = new List<FinderPreviewSpan>(Math.Max(1, tokens.Count));

        foreach (var token in tokens)
        {
            var start = Math.Min(token.StartIndex, line.Length);
            var end = Math.Min(token.EndIndex, line.Length);
            if (end <= start)
            {
                continue;
            }

            var text = line[start..end];
            AppendSpan(spans, text, GetStyle(token.Scopes));
        }

        if (spans.Count == 0)
        {
            return FinderPreviewLine.Plain(line);
        }

        return new FinderPreviewLine(spans);
    }

    private static void AppendSpan(List<FinderPreviewSpan> spans, string text, Style? style)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (spans.Count > 0 && Equals(spans[^1].Style, style))
        {
            spans[^1] = spans[^1] with { Text = spans[^1].Text + text };
            return;
        }

        spans.Add(new FinderPreviewSpan(text, style));
    }

    private static Style? GetStyle(IReadOnlyList<string> scopes)
    {
        if (HasScope(scopes, "comment"))
        {
            return CommentStyle;
        }

        if (HasScope(scopes, "string") || HasScope(scopes, "constant.character"))
        {
            return StringStyle;
        }

        if (HasScope(scopes, "constant.numeric"))
        {
            return NumberStyle;
        }

        if (HasScope(scopes, "keyword") || HasScope(scopes, "storage.type") || HasScope(scopes, "storage.modifier"))
        {
            return KeywordStyle;
        }

        if (HasScope(scopes, "entity.name.type") || HasScope(scopes, "support.type") || HasScope(scopes, "support.class"))
        {
            return TypeStyle;
        }

        if (HasScope(scopes, "entity.name.function")
            || HasScope(scopes, "entity.name.method")
            || HasScope(scopes, "variable.function")
            || HasScope(scopes, "support.function")
            || HasScope(scopes, "meta.function-call"))
        {
            return FunctionStyle;
        }

        if (HasScope(scopes, "keyword.operator") || HasScope(scopes, "punctuation"))
        {
            return OperatorStyle;
        }

        return null;
    }

    private static bool HasScope(IReadOnlyList<string> scopes, string fragment)
    {
        for (var index = 0; index < scopes.Count; index++)
        {
            if (scopes[index].Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
