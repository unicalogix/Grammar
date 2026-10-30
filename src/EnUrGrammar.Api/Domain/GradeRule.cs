namespace EnUrGrammar.Api.Domain;

public class GradeRule
{
    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
    public int RuleItemId { get; set; }
    public RuleItem RuleItem { get; set; } = null!;
}
