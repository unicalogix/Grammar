namespace EnUrGrammar.Api.Domain;

public class SentenceRule
{
    public int SentenceId { get; set; }
    public Sentence Sentence { get; set; } = null!;
    public int RuleDetailId { get; set; }
    public RuleDetail RuleDetail { get; set; } = null!;
}
