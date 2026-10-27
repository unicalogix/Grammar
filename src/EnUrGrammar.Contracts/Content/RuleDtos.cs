namespace EnUrGrammar.Contracts.Content;

public sealed record RuleDto(
    int Id,
    string NameEn,
    string? NameUr,
    string? ExplanationEn,
    string? ExplanationUr,
    string? Code,
    bool IsActive,
    IReadOnlyList<int> GradeIds);

public sealed record RuleUpsertRequest(
    string NameEn,
    string? NameUr,
    string? ExplanationEn,
    string? ExplanationUr,
    string? Code,
    bool IsActive);
