namespace TextBanner.Services;

public static class TextMatcher
{
    /// <summary>把匹配文本按 | ; 换行 拆成“任一命中”的多个词条。</summary>
    public static string[] SplitTerms(string text)
        => (text ?? "").Split(new[] { '|', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>haystack 包含任一匹配词条时返回命中的词条，否则 null（不区分大小写）。</summary>
    public static string MatchAny(string haystack, string matchText)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(matchText)) return null;
        foreach (var term in SplitTerms(matchText))
        {
            if (term.Length > 0 && haystack.Contains(term, StringComparison.OrdinalIgnoreCase))
                return term;
        }
        return null;
    }

    /// <summary>来源限定：haystack 包含任意限定词条即通过；限定为空则恒通过。</summary>
    public static bool ContainsFilter(string haystack, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        if (string.IsNullOrEmpty(haystack)) return false;
        foreach (var term in SplitTerms(filter))
        {
            if (haystack.Contains(term, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
