namespace EnUrGrammar.Api.Domain;

public enum Difficulty : byte
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public enum SentenceStatus : byte
{
    Draft = 0,
    Approved = 1
}

public enum ImportEntityKind : byte
{
    Grades = 1,
    Rules = 2,
    Sentences = 3
}

public enum ImportBatchStatus : byte
{
    Pending = 0,
    Validated = 1,
    Committed = 2,
    Failed = 3
}

public enum ExerciseType : byte
{
    MultiChoice = 1,
    Arrange = 2,
    FreeType = 3,
    FillBlank = 4,
    ArrangeDistractors = 5,
    RulesApply = 6,
    /// <summary>Grid layout single-select dropdown (rules or translation per DropdownMode).</summary>
    GridDropdown = 7
}

public enum TestLayout : byte
{
    Sequential = 1,
    Grid = 2
}

/// <summary>For Grid layout: what each question's dropdown shows.</summary>
public enum DropdownMode : byte
{
    None = 0,
    Rules = 1,
    Translation = 2
}
