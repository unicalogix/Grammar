namespace EnUrGrammar.Contracts.Import;

public sealed record ImportRowError(int RowNumber, string Field, string Message);

public sealed record ImportBatchDto(
    int Id,
    string FileName,
    string? ContentType,
    byte EntityKind,
    byte Status,
    int RowCount,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<ImportRowError> Errors,
    IReadOnlyList<IDictionary<string, string?>> PreviewRows,
    DateTime CreatedAtUtc,
    DateTime? CommittedAtUtc);

public sealed record ImportCommitResult(int BatchId, int SuccessCount, byte Status);
