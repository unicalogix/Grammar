namespace EnUrGrammar.Api.Domain;

public class PracticeSession
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = "en";
    public string TargetLanguage { get; set; } = "ur";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    /// <summary>Snapshot of filters + planned sentence/exercise sequence.</summary>
    public string? FilterJson { get; set; }
    public int ItemCount { get; set; }

    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
