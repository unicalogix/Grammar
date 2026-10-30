namespace EnUrGrammar.Api.Domain;

public class GradeRule
{
    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
    public int RuleDetailId { get; set; }
    public RuleDetail RuleDetail { get; set; } = null!;
}
