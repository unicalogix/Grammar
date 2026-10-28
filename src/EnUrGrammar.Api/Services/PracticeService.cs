using System.Text.Json;
using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Contracts.Practice;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Services;

public class PracticeService
{
    public const int DefaultItemCount = 8;
    public const int MaxItemCount = 20;
    public const int McqOptionCount = 4;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly AppDbContext _db;

    public PracticeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PracticeCatalogDto> GetCatalogAsync(CancellationToken ct)
    {
        var grades = await _db.Grades.AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.NameEn)
            .Select(g => new PracticeGradeOptionDto(g.Id, g.NameEn, g.NameUr, g.Code, g.SortOrder))
            .ToListAsync(ct);

        var rules = await _db.Rules.AsNoTracking()
            .Where(r => r.IsActive)
            .Include(r => r.GradeRules)
            .OrderBy(r => r.NameEn)
            .ToListAsync(ct);

        var ruleDtos = rules.Select(r => new PracticeRuleOptionDto(
            r.Id, r.NameEn, r.NameUr, r.Code,
            r.GradeRules.Select(gr => gr.GradeId).ToList())).ToList();

        return new PracticeCatalogDto(grades, ruleDtos);
    }

    public async Task<(PracticeSessionDto dto, string? error)> StartAsync(
        string userId, StartPracticeSessionRequest req, CancellationToken ct)
    {
        if (req.GradeIds is null || req.GradeIds.Count == 0)
            return (null!, "Select at least one grade.");

        var source = NormalizeLang(req.SourceLanguage);
        if (source is null)
            return (null!, "sourceLanguage must be 'en' or 'ur'.");

        var target = source == "en" ? "ur" : "en";
        var itemCount = Math.Clamp(req.ItemCount ?? DefaultItemCount, 1, MaxItemCount);

        Difficulty? difficulty = null;
        if (req.Difficulty is not null)
        {
            if (!Enum.IsDefined(typeof(Difficulty), req.Difficulty.Value))
                return (null!, "Difficulty must be 1 (Easy), 2 (Medium), or 3 (Hard).");
            difficulty = (Difficulty)req.Difficulty.Value;
        }

        var gradeIds = req.GradeIds.Distinct().ToList();
        var ruleIds = (req.RuleIds ?? []).Distinct().ToList();

        var q = _db.Sentences.AsNoTracking()
            .Where(s => !s.IsDeleted
                        && s.Status == SentenceStatus.Approved
                        && gradeIds.Contains(s.GradeId));

        if (difficulty is not null)
            q = q.Where(s => s.Difficulty == difficulty);

        if (ruleIds.Count > 0)
            q = q.Where(s => s.SentenceRules.Any(sr => ruleIds.Contains(sr.RuleId)));

        var pool = await q.Select(s => new { s.Id, s.TextEn, s.TextUr }).ToListAsync(ct);
        if (pool.Count == 0)
        {
            return (new PracticeSessionDto(
                0, source, target, 0, DateTime.UtcNow, null,
                "No approved sentences match these filters. Ask a teacher to approve content, or widen grades/rules/difficulty.",
                null), null);
        }

        var rng = Random.Shared;
        var picked = pool.OrderBy(_ => rng.Next()).Take(itemCount).ToList();
        var exerciseCycle = new[] { ExerciseType.MultiChoice, ExerciseType.Arrange, ExerciseType.FreeType };
        var planItems = picked.Select((s, i) => new PlanItem(s.Id, exerciseCycle[i % exerciseCycle.Length])).ToList();

        var plan = new SessionPlan(
            gradeIds, ruleIds, req.Difficulty, source, target,
            planItems);

        var session = new PracticeSession
        {
            UserId = userId,
            SourceLanguage = source,
            TargetLanguage = target,
            StartedAtUtc = DateTime.UtcNow,
            ItemCount = planItems.Count,
            FilterJson = JsonSerializer.Serialize(plan, JsonOpts)
        };
        _db.PracticeSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        var first = await BuildItemAsync(session, plan, 1, ct);
        return (new PracticeSessionDto(
            session.Id, source, target, session.ItemCount, session.StartedAtUtc, null, null, first), null);
    }

    public async Task<(AttemptResultDto? dto, string? error)> SubmitAttemptAsync(
        string userId, int sessionId, SubmitAttemptRequest req, CancellationToken ct)
    {
        var session = await _db.PracticeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        if (session is null)
            return (null, "Session not found.");
        if (session.EndedAtUtc is not null)
            return (null, "Session already completed.");

        var plan = DeserializePlan(session.FilterJson);
        if (plan is null || plan.Items.Count == 0)
            return (null, "Session has no practice plan.");

        if (req.SequenceNo < 1 || req.SequenceNo > plan.Items.Count)
            return (null, "Invalid sequenceNo.");

        var attempted = await _db.Attempts.CountAsync(a => a.PracticeSessionId == sessionId, ct);
        if (req.SequenceNo != attempted + 1)
            return (null, $"Expected sequenceNo {attempted + 1}.");

        var planItem = plan.Items[req.SequenceNo - 1];
        if (planItem.SentenceId != req.SentenceId)
            return (null, "SentenceId does not match the current item.");
        if ((byte)planItem.ExerciseType != req.ExerciseType)
            return (null, "ExerciseType does not match the current item.");

        var sentence = await _db.Sentences.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == planItem.SentenceId && !s.IsDeleted, ct);
        if (sentence is null)
            return (null, "Sentence not found.");

        var expected = plan.TargetLanguage == "en" ? sentence.TextEn : sentence.TextUr;
        var studentRaw = req.StudentAnswer ?? string.Empty;
        var (isCorrect, normExpected, normStudent, studentStored) =
            Score(planItem.ExerciseType, expected, studentRaw, plan.TargetLanguage);

        var attempt = new Attempt
        {
            PracticeSessionId = sessionId,
            UserId = userId,
            SentenceId = sentence.Id,
            ExerciseType = planItem.ExerciseType,
            PromptLanguage = plan.SourceLanguage,
            ExpectedAnswer = expected,
            StudentAnswer = Truncate(studentStored, 500),
            NormalizedExpected = Truncate(normExpected, 500),
            NormalizedStudent = Truncate(normStudent, 500),
            IsCorrect = isCorrect,
            AnsweredAtUtc = DateTime.UtcNow,
            SequenceNo = req.SequenceNo
        };
        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        PracticeItemDto? next = null;
        var complete = req.SequenceNo >= plan.Items.Count;
        if (complete)
        {
            session.EndedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            next = await BuildItemAsync(session, plan, req.SequenceNo + 1, ct);
        }

        return (new AttemptResultDto(
            attempt.Id, attempt.SequenceNo, isCorrect, expected, attempt.StudentAnswer, next, complete), null);
    }

    public async Task<(PracticeSessionDto? dto, string? error)> CompleteAsync(
        string userId, int sessionId, CancellationToken ct)
    {
        var session = await _db.PracticeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        if (session is null)
            return (null, "Session not found.");
        session.EndedAtUtc ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (new PracticeSessionDto(
            session.Id, session.SourceLanguage, session.TargetLanguage,
            session.ItemCount, session.StartedAtUtc, session.EndedAtUtc, null, null), null);
    }

    private async Task<PracticeItemDto> BuildItemAsync(
        PracticeSession session, SessionPlan plan, int sequenceNo, CancellationToken ct)
    {
        var planItem = plan.Items[sequenceNo - 1];
        var sentence = await _db.Sentences.AsNoTracking()
            .FirstAsync(s => s.Id == planItem.SentenceId, ct);

        var prompt = plan.SourceLanguage == "en" ? sentence.TextEn : sentence.TextUr;
        var expected = plan.TargetLanguage == "en" ? sentence.TextEn : sentence.TextUr;

        IReadOnlyList<string>? options = null;
        IReadOnlyList<string>? tokens = null;

        switch (planItem.ExerciseType)
        {
            case ExerciseType.MultiChoice:
                options = await BuildMcqOptionsAsync(expected, sentence.Id, plan.TargetLanguage, ct);
                break;
            case ExerciseType.Arrange:
                tokens = ShuffleTokens(AnswerNormalization.Tokenize(expected));
                break;
            case ExerciseType.FreeType:
                break;
        }

        return new PracticeItemDto(
            sequenceNo,
            sentence.Id,
            (byte)planItem.ExerciseType,
            plan.SourceLanguage,
            prompt,
            options,
            tokens);
    }

    private async Task<IReadOnlyList<string>> BuildMcqOptionsAsync(
        string correct, int sentenceId, string targetLang, CancellationToken ct)
    {
        var distractorQuery = _db.Sentences.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Status == SentenceStatus.Approved && s.Id != sentenceId);

        var candidates = targetLang == "en"
            ? await distractorQuery.Select(s => s.TextEn).Take(80).ToListAsync(ct)
            : await distractorQuery.Select(s => s.TextUr).Take(80).ToListAsync(ct);

        var unique = candidates
            .Where(t => !string.IsNullOrWhiteSpace(t)
                        && !string.Equals(t.Trim(), correct.Trim(), StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(_ => Random.Shared.Next())
            .Take(McqOptionCount - 1)
            .ToList();

        // Pad with mild variants if pool is thin
        while (unique.Count < McqOptionCount - 1)
        {
            unique.Add($"{correct} ({unique.Count + 1})");
        }

        var options = unique.Append(correct).OrderBy(_ => Random.Shared.Next()).ToList();
        return options;
    }

    private static IReadOnlyList<string> ShuffleTokens(IReadOnlyList<string> tokens)
    {
        var list = tokens.ToList();
        if (list.Count <= 1) return list;
        // Ensure not already correct order when possible
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var shuffled = list.OrderBy(_ => Random.Shared.Next()).ToList();
            if (!AnswerNormalization.TokensEqual(list, shuffled) || list.Count < 2)
                return shuffled;
        }
        return list.OrderBy(_ => Random.Shared.Next()).ToList();
    }

    private static (bool isCorrect, string normExpected, string normStudent, string studentStored)
        Score(ExerciseType type, string expected, string studentRaw, string targetLang)
    {
        switch (type)
        {
            case ExerciseType.Arrange:
            {
                var expectedTokens = AnswerNormalization.Tokenize(expected);
                var studentTokens = AnswerNormalization.Tokenize(studentRaw);
                var ok = AnswerNormalization.TokensEqual(expectedTokens, studentTokens);
                var joined = string.Join(' ', studentTokens);
                return (ok, string.Join(' ', expectedTokens), joined, joined);
            }
            case ExerciseType.MultiChoice:
            case ExerciseType.FreeType:
            default:
            {
                var ne = AnswerNormalization.Normalize(expected, targetLang);
                var ns = AnswerNormalization.Normalize(studentRaw, targetLang);
                return (string.Equals(ne, ns, StringComparison.Ordinal), ne, ns, studentRaw.Trim());
            }
        }
    }

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

    private static SessionPlan? DeserializePlan(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<SessionPlan>(json, JsonOpts);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private sealed record SessionPlan(
        List<int> GradeIds,
        List<int> RuleIds,
        byte? Difficulty,
        string SourceLanguage,
        string TargetLanguage,
        List<PlanItem> Items);

    private sealed record PlanItem(int SentenceId, ExerciseType ExerciseType);
}
