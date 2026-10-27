namespace EnUrGrammar.Contracts.Content;

public sealed record SentenceDto(
    int Id,
    string TextEn,
    string TextUr,
    int GradeId,
    string GradeNameEn,
    byte Difficulty,
    byte Status,
    string? MtSuggestedUr,
    string? MtSuggestedEn,
    DateTime? MtConfirmedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    IReadOnlyList<int> RuleIds,
    IReadOnlyList<string> RuleCodes);

public sealed record SentenceUpsertRequest(
    string TextEn,
    string TextUr,
    int GradeId,
    byte Difficulty,
    byte? Status,
    IReadOnlyList<int>? RuleIds);

public sealed record SetSentenceRulesRequest(IReadOnlyList<int> RuleIds);

public sealed record SentenceApproveRequest(bool Approve);
