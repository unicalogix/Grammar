namespace EnUrGrammar.Api.Domain;

/// <summary>Master rule (e.g. "Tense", "Types of Speech") with one-to-many details.</summary>
public class Rule
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameUr { get; set; }
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }

    public ICollection<RuleDetail> Details { get; set; } = new List<RuleDetail>();
}
