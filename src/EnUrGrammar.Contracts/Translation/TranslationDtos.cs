namespace EnUrGrammar.Contracts.Translation;

public sealed record SuggestTranslationRequest(
    string? TextEn,
    string? TextUr,
    string TargetLanguage);

public sealed record SuggestTranslationResponse(
    string? SuggestedEn,
    string? SuggestedUr,
    string Provider,
    bool IsStub);
