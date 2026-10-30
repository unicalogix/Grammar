namespace EnUrGrammar.Api.Domain;

public class SentenceRule
{
    public int SentenceId { get; set; }
    public Sentence Sentence { get; set; } = null!;
    public int RuleItemId { get; set; }
    public RuleItem RuleItem { get; set; } = null!;
}
