namespace EnUrGrammar.Api.Domain;

public class SentenceRule
{
    public int SentenceId { get; set; }
    public Sentence Sentence { get; set; } = null!;
    public int RuleId { get; set; }
    public Rule Rule { get; set; } = null!;
}
