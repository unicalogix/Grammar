using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EnUrGrammar.Api.Services;

/// <summary>
/// Free-type / answer comparison pipeline (v1 schema §3.2).
/// </summary>
public static class AnswerNormalization
{
    private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);
    private static readonly char[] TrailingPunct = ['.', '?', '!', '؟', '۔', '…'];

    public static string Normalize(string? raw, string language)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var s = raw.Normalize(NormalizationForm.FormC);
        s = MultiSpace.Replace(s.Trim(), " ");

        var lang = language.Trim().ToLowerInvariant();
        if (lang is "en" or "eng" or "english")
        {
            s = s.ToLowerInvariant();
        }
        else
        {
            // Urdu / other: strip tatweel; keep letter case as-is
            s = s.Replace("\u0640", string.Empty); // tatweel ـ
            s = StripUrduDiacritics(s);
        }

        s = s.TrimEnd(TrailingPunct).Trim();
        return s;
    }

    public static bool EqualsNormalized(string? expected, string? student, string language) =>
        string.Equals(Normalize(expected, language), Normalize(student, language), StringComparison.Ordinal);

    public static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        return MultiSpace.Replace(text.Trim(), " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool TokensEqual(IReadOnlyList<string> expected, IReadOnlyList<string> student)
    {
        if (expected.Count != student.Count) return false;
        for (var i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(expected[i], student[i], StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static string StripUrduDiacritics(string s)
    {
        // Optional v1: strip Arabic combining marks / short vowels (U+064B–U+065F, U+0670)
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s.Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark)
                continue;
            if (ch is >= '\u064B' and <= '\u065F' or '\u0670')
                continue;
            sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
