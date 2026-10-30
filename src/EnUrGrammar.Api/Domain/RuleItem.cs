namespace EnUrGrammar.Api.Domain;

/// <summary>Leaf rule linked to sentences (e.g. "Simple Present", "Direct Speech").</summary>
public class RuleItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public RuleCategory Category { get; set; } = null!;
    public string NameEn { get; set; } = string.Empty;
    public string? NameUr { get; set; }
    public string? ExplanationEn { get; set; }
    public string? ExplanationUr { get; set; }
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }

    public ICollection<GradeRule> GradeRules { get; set; } = new List<GradeRule>();
}
