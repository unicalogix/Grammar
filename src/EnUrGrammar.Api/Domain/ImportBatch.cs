namespace EnUrGrammar.Api.Domain;

public class ImportBatch
{
    public int Id { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public ImportEntityKind EntityKind { get; set; }
    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Pending;
    public int RowCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorReportJson { get; set; }
    public string? PreviewJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CommittedAtUtc { get; set; }
}
