namespace EnUrGrammar.Api.Domain;

public class GradeRule
{
    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
    public int RuleId { get; set; }
    public Rule Rule { get; set; } = null!;
}
