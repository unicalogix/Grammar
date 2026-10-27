namespace EnUrGrammar.Api.Domain;

public class Grade
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameUr { get; set; }
    /// <summary>Optional stable import key (e.g. G5).</summary>
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }

    public ICollection<GradeRule> GradeRules { get; set; } = new List<GradeRule>();
    public ICollection<Sentence> Sentences { get; set; } = new List<Sentence>();
}
