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
