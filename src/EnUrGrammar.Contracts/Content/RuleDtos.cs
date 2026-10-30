namespace EnUrGrammar.Contracts.Content;

/// <summary>Master rule with nested detail rows (admin authoring).</summary>
public sealed record RuleDto(
    int Id,
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<RuleDetailSummaryDto> Details);

public sealed record RuleDetailSummaryDto(
    int Id,
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive);

public sealed record RuleWithDetailsUpsertRequest(
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<RuleDetailUpsertInRuleRequest>? Details);

public sealed record RuleDetailUpsertInRuleRequest(
    int? Id,
    string NameEn,
    string? NameUr,
    string? ExplanationEn,
    string? ExplanationUr,
    string? Code,
    int SortOrder,
    bool IsActive);

/// <summary>Flat rule detail used by sentences, grades, practice filters, and grid dropdowns.</summary>
public sealed record RuleDetailDto(
    int Id,
    int RuleId,
    string RuleNameEn,
    string NameEn,
    string? NameUr,
    string? ExplanationEn,
    string? ExplanationUr,
    string? Code,
    bool IsActive,
    IReadOnlyList<int> GradeIds);

public sealed record RuleDetailUpsertRequest(
    int RuleId,
    string NameEn,
    string? NameUr,
    string? ExplanationEn,
    string? ExplanationUr,
    string? Code,
    bool IsActive,
    int SortOrder = 0);
