namespace EnUrGrammar.Contracts.Practice;

public sealed record SavedTestDto(
    int Id,
    string Name,
    string? Description,
    byte Layout,
    byte DropdownMode,
    IReadOnlyList<byte> ExerciseTypes,
    IReadOnlyList<int> GradeIds,
    IReadOnlyList<int> RuleIds,
    byte? Difficulty,
    string SourceLanguage,
    int ItemCount,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record SavedTestSummaryDto(
    int Id,
    string Name,
    string? Description,
    byte Layout,
    byte DropdownMode,
    IReadOnlyList<byte> ExerciseTypes,
    int ItemCount,
    bool IsActive);

public sealed record UpsertSavedTestRequest(
    string Name,
    string? Description,
    byte Layout,
    byte DropdownMode,
    IReadOnlyList<byte> ExerciseTypes,
    IReadOnlyList<int> GradeIds,
    IReadOnlyList<int>? RuleIds,
    byte? Difficulty,
    string SourceLanguage,
    int ItemCount,
    bool IsActive = true);
