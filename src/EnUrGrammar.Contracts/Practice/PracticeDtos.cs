namespace EnUrGrammar.Contracts.Practice;

public sealed record PracticeCatalogDto(
    IReadOnlyList<PracticeGradeOptionDto> Grades,
    IReadOnlyList<PracticeRuleOptionDto> Rules);

public sealed record PracticeGradeOptionDto(
    int Id,
    string NameEn,
    string? NameUr,
    string? Code,
    int SortOrder);

public sealed record PracticeRuleOptionDto(
    int Id,
    string NameEn,
    string? NameUr,
    string? Code,
    IReadOnlyList<int> GradeIds);

public sealed record StartPracticeSessionRequest(
    IReadOnlyList<int> GradeIds,
    IReadOnlyList<int>? RuleIds,
    byte? Difficulty,
    string SourceLanguage,
    int? ItemCount);

public sealed record PracticeSessionDto(
    int Id,
    string SourceLanguage,
    string TargetLanguage,
    int ItemCount,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string? EmptyMessage,
    PracticeItemDto? Item,
    /// <summary>All items for Grid layout; null/empty for Sequential (use Item).</summary>
    IReadOnlyList<PracticeItemDto>? Items = null,
    int? SavedTestId = null,
    byte? Layout = null,
    string? TestName = null);

public sealed record PracticeItemDto(
    int SequenceNo,
    int SentenceId,
    byte ExerciseType,
    string PromptLanguage,
    string PromptText,
    /// <summary>MCQ options, fill-blank word bank, or rule labels (RulesApply).</summary>
    IReadOnlyList<string>? Options,
    /// <summary>Shuffled tokens for arrange / arrange-with-distractors.</summary>
    IReadOnlyList<string>? Tokens,
    /// <summary>Target sentence with ____ for FillBlank.</summary>
    string? BlankedText = null,
    /// <summary>Rule ids parallel to Options for RulesApply multi-select.</summary>
    IReadOnlyList<int>? OptionIds = null);

public sealed record SubmitAttemptRequest(
    int SequenceNo,
    int SentenceId,
    byte ExerciseType,
    /// <summary>
    /// Selected option text; space-joined arrange order; free-typed answer;
    /// fill-blank word; or comma-separated rule ids for RulesApply.
    /// </summary>
    string StudentAnswer);

public sealed record AttemptResultDto(
    int AttemptId,
    int SequenceNo,
    bool IsCorrect,
    string ExpectedAnswer,
    string StudentAnswer,
    PracticeItemDto? NextItem,
    bool SessionComplete,
    /// <summary>Correct option id(s) for grid rules dropdown (when wrong / reveal).</summary>
    IReadOnlyList<int>? CorrectOptionIds = null);
