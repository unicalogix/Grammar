namespace EnUrGrammar.Api.Domain;

public class Sentence
{
    public int Id { get; set; }
    public string TextEn { get; set; } = string.Empty;
    public string TextUr { get; set; } = string.Empty;
    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;
    public SentenceStatus Status { get; set; } = SentenceStatus.Draft;
    public string? MtSuggestedUr { get; set; }
    public string? MtSuggestedEn { get; set; }
    public DateTime? MtConfirmedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? ApprovedByUserId { get; set; }

    public ICollection<SentenceRule> SentenceRules { get; set; } = new List<SentenceRule>();
}
