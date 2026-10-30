namespace EnUrGrammar.Api.Domain;

/// <summary>
/// Reusable test definition: layout, exercise types, filters, and grid dropdown mode.
/// Admin/Teacher author; Student takes via practice session start.
/// </summary>
public class SavedTest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TestLayout Layout { get; set; } = TestLayout.Sequential;
    /// <summary>Required for Grid; ignored for Sequential.</summary>
    public DropdownMode DropdownMode { get; set; } = DropdownMode.None;
    /// <summary>JSON array of ExerciseType byte values, e.g. [1,4,5]. Used by Sequential.</summary>
    public string ExerciseTypesJson { get; set; } = "[]";
    /// <summary>JSON array of grade ids.</summary>
    public string GradeIdsJson { get; set; } = "[]";
    /// <summary>JSON array of rule item ids (optional filter).</summary>
    public string? RuleIdsJson { get; set; }
    public byte? Difficulty { get; set; }
    /// <summary>Answer direction: en→ur or ur→en. Derived from prompt checkboxes when saving.</summary>
    public string SourceLanguage { get; set; } = "en";
    /// <summary>Show English sentence text on questions.</summary>
    public bool ShowEnglish { get; set; } = true;
    /// <summary>Show Urdu (translation) sentence text on questions.</summary>
    public bool ShowTranslation { get; set; }
    public int ItemCount { get; set; } = 8;
    public bool IsActive { get; set; } = true;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
