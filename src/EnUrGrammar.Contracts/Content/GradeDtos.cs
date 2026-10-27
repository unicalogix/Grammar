namespace EnUrGrammar.Contracts.Content;

public sealed record GradeDto(
    int Id,
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<int> RuleIds);

public sealed record GradeUpsertRequest(
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive);

public sealed record SetGradeRulesRequest(IReadOnlyList<int> RuleIds);
