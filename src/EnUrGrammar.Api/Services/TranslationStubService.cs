using EnUrGrammar.Contracts.Translation;

namespace EnUrGrammar.Api.Services;

/// <summary>
/// Phase 1 stub: returns a clearly marked placeholder suggestion.
/// Never publishes or changes sentence Status.
/// </summary>
public class TranslationStubService
{
    public SuggestTranslationResponse Suggest(SuggestTranslationRequest request)
    {
        var target = (request.TargetLanguage ?? "ur").Trim().ToLowerInvariant();
        string? suggestedEn = null;
        string? suggestedUr = null;

        if (target is "ur" or "urdu")
        {
            suggestedUr = string.IsNullOrWhiteSpace(request.TextEn)
                ? null
                : $"[MT stub] {request.TextEn}";
        }
        else if (target is "en" or "english")
        {
            suggestedEn = string.IsNullOrWhiteSpace(request.TextUr)
                ? null
                : $"[MT stub] {request.TextUr}";
        }
        else
        {
            // Suggest both directions when unspecified
            if (!string.IsNullOrWhiteSpace(request.TextEn))
                suggestedUr = $"[MT stub] {request.TextEn}";
            if (!string.IsNullOrWhiteSpace(request.TextUr))
                suggestedEn = $"[MT stub] {request.TextUr}";
        }

        return new SuggestTranslationResponse(suggestedEn, suggestedUr, "stub", IsStub: true);
    }
}
