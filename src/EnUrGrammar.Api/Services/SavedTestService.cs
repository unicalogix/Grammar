using System.Text.Json;
using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Contracts.Practice;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Services;

public class SavedTestService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly HashSet<byte> ValidExerciseTypes =
    [
        (byte)ExerciseType.MultiChoice,
        (byte)ExerciseType.Arrange,
        (byte)ExerciseType.FreeType,
        (byte)ExerciseType.FillBlank,
        (byte)ExerciseType.ArrangeDistractors,
        (byte)ExerciseType.RulesApply
    ];

    private readonly AppDbContext _db;

    public SavedTestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SavedTestSummaryDto>> ListAsync(bool activeOnly, CancellationToken ct)
    {
        var q = _db.SavedTests.AsNoTracking().AsQueryable();
        if (activeOnly)
            q = q.Where(t => t.IsActive);

        var rows = await q.OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    public async Task<SavedTestDto?> GetAsync(int id, CancellationToken ct)
    {
        var t = await _db.SavedTests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? null : ToDto(t);
    }

    public async Task<(SavedTestDto? dto, string? error)> CreateAsync(
        string userId, UpsertSavedTestRequest req, CancellationToken ct)
    {
        var (entity, error) = ValidateAndMap(req, userId, existing: null);
        if (error is not null) return (null, error);

        _db.SavedTests.Add(entity!);
        await _db.SaveChangesAsync(ct);
        return (ToDto(entity!), null);
    }

    public async Task<(SavedTestDto? dto, string? error)> UpdateAsync(
        int id, string userId, UpsertSavedTestRequest req, CancellationToken ct)
    {
        var existing = await _db.SavedTests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null) return (null, "Saved test not found.");

        var (mapped, error) = ValidateAndMap(req, userId, existing);
        if (error is not null) return (null, error);

        existing.Name = mapped!.Name;
        existing.Description = mapped.Description;
        existing.Layout = mapped.Layout;
        existing.DropdownMode = mapped.DropdownMode;
        existing.ExerciseTypesJson = mapped.ExerciseTypesJson;
        existing.GradeIdsJson = mapped.GradeIdsJson;
        existing.RuleIdsJson = mapped.RuleIdsJson;
        existing.Difficulty = mapped.Difficulty;
        existing.SourceLanguage = mapped.SourceLanguage;
        existing.ShowEnglish = mapped.ShowEnglish;
        existing.ShowTranslation = mapped.ShowTranslation;
        existing.ItemCount = mapped.ItemCount;
        existing.IsActive = mapped.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return (ToDto(existing), null);
    }

    public async Task<(bool ok, string? error)> DeactivateAsync(int id, CancellationToken ct)
    {
        var existing = await _db.SavedTests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null) return (false, "Saved test not found.");
        existing.IsActive = false;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static (SavedTest? entity, string? error) ValidateAndMap(
        UpsertSavedTestRequest req, string userId, SavedTest? existing)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return (null, "Name is required.");
        if (req.Name.Trim().Length > 200)
            return (null, "Name must be at most 200 characters.");

        if (!Enum.IsDefined(typeof(TestLayout), req.Layout))
            return (null, "Layout must be 1 (Sequential) or 2 (Grid).");

        var layout = (TestLayout)req.Layout;
        var dropdown = Enum.IsDefined(typeof(DropdownMode), req.DropdownMode)
            ? (DropdownMode)req.DropdownMode
            : DropdownMode.None;

        if (layout == TestLayout.Grid)
        {
            if (dropdown is not (DropdownMode.Rules or DropdownMode.Translation))
                return (null, "Grid layout requires dropdownMode 1 (Rules) or 2 (Translation).");
        }
        else
        {
            dropdown = DropdownMode.None;
        }

        if (req.GradeIds is null || req.GradeIds.Count == 0)
            return (null, "Select at least one grade.");

        var showEnglish = req.ShowEnglish;
        var showTranslation = req.ShowTranslation;
        if (!showEnglish && !showTranslation)
            return (null, "Select at least one prompt language (English and/or Translation).");

        // Answer direction: English-only or both → en→ur; Translation-only → ur→en.
        // Explicit sourceLanguage still wins when valid.
        var source = NormalizeLang(req.SourceLanguage)
                     ?? (showEnglish ? "en" : "ur");

        if (req.Difficulty is not null && !Enum.IsDefined(typeof(Difficulty), req.Difficulty.Value))
            return (null, "Difficulty must be 1 (Easy), 2 (Medium), or 3 (Hard).");

        var itemCount = Math.Clamp(req.ItemCount <= 0 ? PracticeService.DefaultItemCount : req.ItemCount,
            1, PracticeService.MaxItemCount);

        var exerciseTypes = (req.ExerciseTypes ?? [])
            .Where(ValidExerciseTypes.Contains)
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        if (layout == TestLayout.Sequential && exerciseTypes.Count == 0)
            return (null, "Sequential tests need at least one exercise type.");

        if (layout == TestLayout.Grid)
            exerciseTypes = [];

        var gradeIds = req.GradeIds.Distinct().OrderBy(i => i).ToList();
        var ruleIds = (req.RuleIds ?? []).Distinct().OrderBy(i => i).ToList();

        var entity = existing ?? new SavedTest
        {
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        entity.Name = req.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        entity.Layout = layout;
        entity.DropdownMode = dropdown;
        entity.ExerciseTypesJson = JsonSerializer.Serialize(exerciseTypes, JsonOpts);
        entity.GradeIdsJson = JsonSerializer.Serialize(gradeIds, JsonOpts);
        entity.RuleIdsJson = ruleIds.Count == 0 ? null : JsonSerializer.Serialize(ruleIds, JsonOpts);
        entity.Difficulty = req.Difficulty;
        entity.SourceLanguage = source;
        entity.ShowEnglish = showEnglish;
        entity.ShowTranslation = showTranslation;
        entity.ItemCount = itemCount;
        entity.IsActive = req.IsActive;
        if (existing is null)
            entity.CreatedByUserId = userId;

        return (entity, null);
    }

    internal static List<int> ParseIntList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json, JsonOpts) ?? [];
        }
        catch
        {
            return [];
        }
    }

    internal static List<byte> ParseByteList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<byte>>(json, JsonOpts) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static SavedTestDto ToDto(SavedTest t) => new(
        t.Id,
        t.Name,
        t.Description,
        (byte)t.Layout,
        (byte)t.DropdownMode,
        ParseByteList(t.ExerciseTypesJson),
        ParseIntList(t.GradeIdsJson),
        ParseIntList(t.RuleIdsJson),
        t.Difficulty,
        t.SourceLanguage,
        t.ShowEnglish,
        t.ShowTranslation,
        t.ItemCount,
        t.IsActive,
        t.CreatedAtUtc,
        t.UpdatedAtUtc);

    private static SavedTestSummaryDto ToSummary(SavedTest t) => new(
        t.Id,
        t.Name,
        t.Description,
        (byte)t.Layout,
        (byte)t.DropdownMode,
        ParseByteList(t.ExerciseTypesJson),
        t.ItemCount,
        t.IsActive);

    private static string? NormalizeLang(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().ToLowerInvariant();
        return s switch
        {
            "en" or "eng" or "english" => "en",
            "ur" or "urd" or "urdu" => "ur",
            _ => null
        };
    }
}
