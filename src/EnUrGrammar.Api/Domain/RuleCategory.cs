namespace EnUrGrammar.Api.Domain;

/// <summary>Parent group for rule items (e.g. "Tense", "Direct and Indirect Speech").</summary>
public class RuleCategory
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

    public ICollection<RuleItem> Items { get; set; } = new List<RuleItem>();
}
