namespace EnUrGrammar.Contracts.Content;

/// <summary>Flat rule item (leaf) used by sentences, grades, practice filters, and grid dropdowns.</summary>
public sealed record RuleDto(
    int Id,
    int CategoryId,
    string CategoryNameEn,
    string NameEn,
    string? NameUr,
    string? ExplanationEn,
    string? ExplanationUr,
    string? Code,
    bool IsActive,
    IReadOnlyList<int> GradeIds);

public sealed record RuleUpsertRequest(
    int CategoryId,
    string NameEn,
    string? NameUr,
    string? ExplanationEn,
    string? ExplanationUr,
    string? Code,
    bool IsActive,
    int SortOrder = 0);

public sealed record RuleCategoryDto(
    int Id,
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<RuleItemSummaryDto> Items);

public sealed record RuleItemSummaryDto(
    int Id,
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive);

public sealed record RuleCategoryUpsertRequest(
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive);

public sealed record RuleCategoryWithItemsUpsertRequest(
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<RuleItemUpsertInCategoryRequest>? Items);

public sealed record RuleItemUpsertInCategoryRequest(
    int? Id,
    string NameEn,
    string? NameUr,
    string? ExplanationEn,
    string? ExplanationUr,
    string? Code,
    int SortOrder,
    bool IsActive);
