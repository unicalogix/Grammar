namespace EnUrGrammar.Api.Domain;

public class Attempt
{
    public int Id { get; set; }
    public int PracticeSessionId { get; set; }
    public PracticeSession PracticeSession { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public int SentenceId { get; set; }
    public Sentence Sentence { get; set; } = null!;
    public ExerciseType ExerciseType { get; set; }
    public string PromptLanguage { get; set; } = "en";
    public string ExpectedAnswer { get; set; } = string.Empty;
    public string StudentAnswer { get; set; } = string.Empty;
    public string NormalizedExpected { get; set; } = string.Empty;
    public string NormalizedStudent { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public DateTime AnsweredAtUtc { get; set; } = DateTime.UtcNow;
    public int SequenceNo { get; set; }
}
