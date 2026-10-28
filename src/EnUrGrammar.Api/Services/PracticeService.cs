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
    public const int FillBlankOptionCount = 4;
    public const int RulesApplyOptionCount = 4;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly HashSet<string> EnglishStops = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "to", "of", "in", "on", "at", "for", "and", "or", "but",
        "is", "are", "was", "were", "be", "am", "i", "he", "she", "we", "they", "it",
        "my", "his", "her", "our", "their", "this", "that"
    };

    private static readonly ExerciseType[] ExerciseCycle =
    [
        ExerciseType.MultiChoice,
        ExerciseType.Arrange,
        ExerciseType.FreeType,
        ExerciseType.FillBlank,
        ExerciseType.ArrangeDistractors,
        ExerciseType.RulesApply
    ];

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
        var planItems = new List<PlanItem>(picked.Count);
        for (var i = 0; i < picked.Count; i++)
        {
            var s = picked[i];
            var type = ExerciseCycle[i % ExerciseCycle.Length];
            string? blankWord = null;

            if (type == ExerciseType.FillBlank)
            {
                var targetText = target == "en" ? s.TextEn : s.TextUr;
                var blank = MakeBlank(targetText, target);
                if (blank is null)
                    type = ExerciseType.FreeType;
                else
                    blankWord = blank.Value.BlankWord;
            }

            planItems.Add(new PlanItem(s.Id, type, blankWord));
        }

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
            .Include(s => s.SentenceRules).ThenInclude(sr => sr.Rule)
            .FirstOrDefaultAsync(s => s.Id == planItem.SentenceId && !s.IsDeleted, ct);
        if (sentence is null)
            return (null, "Sentence not found.");

        var expectedFull = plan.TargetLanguage == "en" ? sentence.TextEn : sentence.TextUr;
        var studentRaw = req.StudentAnswer ?? string.Empty;

        string expectedStored;
        bool isCorrect;
        string normExpected;
        string normStudent;
        string studentStored;

        if (planItem.ExerciseType == ExerciseType.RulesApply)
        {
            var correctIds = sentence.SentenceRules.Select(sr => sr.RuleId).OrderBy(id => id).ToList();
            var studentIds = ParseIdList(studentRaw);
            isCorrect = correctIds.Count > 0
                        && correctIds.Count == studentIds.Count
                        && correctIds.SequenceEqual(studentIds);
            expectedStored = FormatRuleLabels(
                sentence.SentenceRules
                    .Where(sr => sr.Rule is not null)
                    .Select(sr => sr.Rule!)
                    .ToList());
            studentStored = Truncate(studentRaw.Trim(), 500);
            normExpected = string.Join(',', correctIds);
            normStudent = string.Join(',', studentIds);
        }
        else if (planItem.ExerciseType == ExerciseType.FillBlank)
        {
            var blank = planItem.BlankWord ?? string.Empty;
            expectedStored = expectedFull;
            (isCorrect, normExpected, normStudent, studentStored) =
                ScoreFillBlank(blank, studentRaw, plan.TargetLanguage);
        }
        else
        {
            expectedStored = expectedFull;
            (isCorrect, normExpected, normStudent, studentStored) =
                Score(planItem.ExerciseType, expectedFull, studentRaw, plan.TargetLanguage);
        }

        var attempt = new Attempt
        {
            PracticeSessionId = sessionId,
            UserId = userId,
            SentenceId = sentence.Id,
            ExerciseType = planItem.ExerciseType,
            PromptLanguage = plan.SourceLanguage,
            ExpectedAnswer = Truncate(expectedStored, 500),
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
            attempt.Id, attempt.SequenceNo, isCorrect, expectedStored, attempt.StudentAnswer, next, complete), null);
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
            .Include(s => s.SentenceRules)
            .FirstAsync(s => s.Id == planItem.SentenceId, ct);

        var prompt = plan.SourceLanguage == "en" ? sentence.TextEn : sentence.TextUr;
        var expected = plan.TargetLanguage == "en" ? sentence.TextEn : sentence.TextUr;

        IReadOnlyList<string>? options = null;
        IReadOnlyList<string>? tokens = null;
        string? blankedText = null;
        IReadOnlyList<int>? optionIds = null;

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
            case ExerciseType.FillBlank:
            {
                var blankWord = planItem.BlankWord ?? string.Empty;
                blankedText = ApplyBlank(expected, blankWord);
                options = await BuildFillBlankOptionsAsync(blankWord, sentence.Id, plan.TargetLanguage, ct);
                break;
            }
            case ExerciseType.ArrangeDistractors:
                tokens = await BuildArrangeWithDistractorsAsync(expected, sentence.Id, plan.TargetLanguage, ct);
                break;
            case ExerciseType.RulesApply:
            {
                var built = await BuildRulesApplyOptionsAsync(sentence, ct);
                options = built.Labels;
                optionIds = built.Ids;
                break;
            }
        }

        return new PracticeItemDto(
            sequenceNo,
            sentence.Id,
            (byte)planItem.ExerciseType,
            plan.SourceLanguage,
            prompt,
            options,
            tokens,
            blankedText,
            optionIds);
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

        while (unique.Count < McqOptionCount - 1)
        {
            unique.Add($"{correct} ({unique.Count + 1})");
        }

        var options = unique.Append(correct).OrderBy(_ => Random.Shared.Next()).ToList();
        return options;
    }

    private async Task<IReadOnlyList<string>> BuildFillBlankOptionsAsync(
        string blankWord, int sentenceId, string targetLang, CancellationToken ct)
    {
        var distractorQuery = _db.Sentences.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Status == SentenceStatus.Approved && s.Id != sentenceId);

        var texts = targetLang == "en"
            ? await distractorQuery.Select(s => s.TextEn).Take(80).ToListAsync(ct)
            : await distractorQuery.Select(s => s.TextUr).Take(80).ToListAsync(ct);

        var blankNorm = AnswerNormalization.Normalize(blankWord, targetLang);
        var wordPool = texts
            .SelectMany(AnswerNormalization.Tokenize)
            .Select(StripTokenPunct)
            .Where(w => w.Length >= 2
                        && !string.Equals(
                            AnswerNormalization.Normalize(w, targetLang),
                            blankNorm,
                            StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(_ => Random.Shared.Next())
            .Take(FillBlankOptionCount - 1)
            .ToList();

        while (wordPool.Count < FillBlankOptionCount - 1)
            wordPool.Add($"…{wordPool.Count + 1}");

        return wordPool.Append(StripTokenPunct(blankWord))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
    }

    private async Task<IReadOnlyList<string>> BuildArrangeWithDistractorsAsync(
        string expected, int sentenceId, string targetLang, CancellationToken ct)
    {
        var correct = AnswerNormalization.Tokenize(expected).ToList();
        var distractorCount = correct.Count >= 4 ? 2 : 1;

        var distractorQuery = _db.Sentences.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Status == SentenceStatus.Approved && s.Id != sentenceId);

        var texts = targetLang == "en"
            ? await distractorQuery.Select(s => s.TextEn).Take(80).ToListAsync(ct)
            : await distractorQuery.Select(s => s.TextUr).Take(80).ToListAsync(ct);

        var correctSet = new HashSet<string>(correct, StringComparer.OrdinalIgnoreCase);
        var extras = texts
            .SelectMany(AnswerNormalization.Tokenize)
            .Where(w => w.Length >= 2 && !correctSet.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(_ => Random.Shared.Next())
            .Take(distractorCount)
            .ToList();

        // Guarantee 1–2 distractors even on thin pools
        var n = 1;
        while (extras.Count < distractorCount)
        {
            var pad = targetLang == "en" ? $"extra{n}" : $"اضافی{n}";
            if (!correctSet.Contains(pad) && extras.All(e => !string.Equals(e, pad, StringComparison.OrdinalIgnoreCase)))
                extras.Add(pad);
            n++;
        }

        return ShuffleTokens(correct.Concat(extras).ToList());
    }

    private async Task<(IReadOnlyList<string> Labels, IReadOnlyList<int> Ids)> BuildRulesApplyOptionsAsync(
        Sentence sentence, CancellationToken ct)
    {
        var correctIds = sentence.SentenceRules.Select(sr => sr.RuleId).ToHashSet();
        var correctRules = await _db.Rules.AsNoTracking()
            .Where(r => r.IsActive && correctIds.Contains(r.Id))
            .ToListAsync(ct);

        var needDistractors = Math.Max(1, RulesApplyOptionCount - Math.Max(1, correctRules.Count));
        var distractorPool = await _db.Rules.AsNoTracking()
            .Where(r => r.IsActive && !correctIds.Contains(r.Id))
            .ToListAsync(ct);
        var distractors = distractorPool
            .OrderBy(_ => Random.Shared.Next())
            .Take(needDistractors)
            .ToList();

        // If catalog is thin, still show correct rules alone
        var combined = correctRules.Concat(distractors)
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        if (combined.Count == 0)
        {
            return (["(no rules linked)"], [0]);
        }

        var labels = combined.Select(FormatRuleLabel).ToList();
        var ids = combined.Select(r => r.Id).ToList();
        return (labels, ids);
    }

    private static IReadOnlyList<string> ShuffleTokens(IReadOnlyList<string> tokens)
    {
        var list = tokens.ToList();
        if (list.Count <= 1) return list;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var shuffled = list.OrderBy(_ => Random.Shared.Next()).ToList();
            if (!AnswerNormalization.TokensEqual(list, shuffled) || list.Count < 2)
                return shuffled;
        }
        return list.OrderBy(_ => Random.Shared.Next()).ToList();
    }

    private static (string BlankWord, string BlankedText)? MakeBlank(string text, string language)
    {
        var tokens = AnswerNormalization.Tokenize(text);
        if (tokens.Count < 2) return null;

        var candidates = new List<(string Word, int Index)>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var raw = tokens[i];
            var word = StripTokenPunct(raw);
            if (word.Length < 2) continue;
            if (language is "en" && EnglishStops.Contains(word)) continue;
            candidates.Add((word, i));
        }

        if (candidates.Count == 0)
        {
            // Prefer a middle token over the first
            for (var i = 1; i < tokens.Count; i++)
            {
                var word = StripTokenPunct(tokens[i]);
                if (word.Length >= 1)
                    candidates.Add((word, i));
            }
        }

        if (candidates.Count == 0) return null;

        var pick = candidates[Random.Shared.Next(candidates.Count)];
        var blanked = tokens.ToList();
        blanked[pick.Index] = "____";
        return (pick.Word, string.Join(' ', blanked));
    }

    private static string ApplyBlank(string text, string blankWord)
    {
        var tokens = AnswerNormalization.Tokenize(text).ToList();
        var blankNorm = AnswerNormalization.Normalize(blankWord, "en"); // case-fold for EN; UR normalize separately below
        for (var i = 0; i < tokens.Count; i++)
        {
            var stripped = StripTokenPunct(tokens[i]);
            if (string.Equals(stripped, blankWord, StringComparison.OrdinalIgnoreCase)
                || string.Equals(AnswerNormalization.Normalize(stripped, "ur"),
                    AnswerNormalization.Normalize(blankWord, "ur"), StringComparison.Ordinal)
                || string.Equals(AnswerNormalization.Normalize(stripped, "en"), blankNorm, StringComparison.Ordinal))
            {
                tokens[i] = "____";
                return string.Join(' ', tokens);
            }
        }

        // Fallback: blank a middle token
        if (tokens.Count >= 2)
        {
            tokens[tokens.Count / 2] = "____";
            return string.Join(' ', tokens);
        }

        return "____";
    }

    private static string StripTokenPunct(string token) =>
        token.Trim().TrimEnd('.', '?', '!', '؟', '۔', '…', ',', ';', ':').Trim();

    private static (bool isCorrect, string normExpected, string normStudent, string studentStored)
        Score(ExerciseType type, string expected, string studentRaw, string targetLang)
    {
        switch (type)
        {
            case ExerciseType.Arrange:
            case ExerciseType.ArrangeDistractors:
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

    private static (bool isCorrect, string normExpected, string normStudent, string studentStored)
        ScoreFillBlank(string blankWord, string studentRaw, string targetLang)
    {
        var ne = AnswerNormalization.Normalize(StripTokenPunct(blankWord), targetLang);
        var ns = AnswerNormalization.Normalize(StripTokenPunct(studentRaw), targetLang);
        return (string.Equals(ne, ns, StringComparison.Ordinal), ne, ns, studentRaw.Trim());
    }

    private static List<int> ParseIdList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split([',', ';', ' ', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => int.TryParse(p, out var id) ? id : (int?)null)
            .Where(id => id is > 0)
            .Select(id => id!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();
    }

    private static string FormatRuleLabel(Rule r) =>
        string.IsNullOrWhiteSpace(r.NameUr) ? r.NameEn : $"{r.NameEn} / {r.NameUr}";

    private static string FormatRuleLabels(IReadOnlyList<Rule> rules) =>
        string.Join("; ", rules.OrderBy(r => r.NameEn).Select(FormatRuleLabel));

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

    private sealed record PlanItem(
        int SentenceId,
        ExerciseType ExerciseType,
        string? BlankWord = null);
}
