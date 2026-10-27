namespace EnUrGrammar.Api.Domain;

public class Rule
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameUr { get; set; }
    public string? ExplanationEn { get; set; }
    public string? ExplanationUr { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }

    public ICollection<GradeRule> GradeRules { get; set; } = new List<GradeRule>();
}
