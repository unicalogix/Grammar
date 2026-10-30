using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Contracts.Import;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Services;

public class ImportService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly AppDbContext _db;

    public ImportService(AppDbContext db) => _db = db;

    public byte[] BuildTemplateCsv(ImportEntityKind kind)
    {
        var csv = kind switch
        {
            ImportEntityKind.Grades => "Code,NameEn,NameUr,SortOrder,IsActive\nG6,Grade 6,جماعت ۶,6,true\n",
            ImportEntityKind.Rules => "CategoryCode,CategoryNameEn,CategoryNameUr,Code,NameEn,NameUr,ExplanationEn,ExplanationUr,IsActive\nTENSE,Tense,زمانہ,PRESENT_SIMPLE,Simple Present,حال مطلق,Habits and facts.,عادت اور حقائق۔,true\n",
            ImportEntityKind.Sentences => "GradeCode,TextEn,TextUr,Difficulty,Status,RuleCodes\nG5,I walked to school.,میں اسکول گیا۔,Easy,Draft,PAST_SIMPLE\n",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
    }

    public byte[] BuildTemplateXlsx(ImportEntityKind kind)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Import");
        var rows = kind switch
        {
            ImportEntityKind.Grades => new[]
            {
                new[] { "Code", "NameEn", "NameUr", "SortOrder", "IsActive" },
                new[] { "G6", "Grade 6", "جماعت ۶", "6", "true" }
            },
            ImportEntityKind.Rules => new[]
            {
                new[] { "CategoryCode", "CategoryNameEn", "CategoryNameUr", "Code", "NameEn", "NameUr", "ExplanationEn", "ExplanationUr", "IsActive" },
                new[] { "TENSE", "Tense", "زمانہ", "PRESENT_SIMPLE", "Simple Present", "حال مطلق", "Habits and facts.", "عادت اور حقائق۔", "true" }
            },
            ImportEntityKind.Sentences => new[]
            {
                new[] { "GradeCode", "TextEn", "TextUr", "Difficulty", "Status", "RuleCodes" },
                new[] { "G5", "I walked to school.", "میں اسکول گیا۔", "Easy", "Draft", "PAST_SIMPLE" }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        for (var r = 0; r < rows.Length; r++)
        for (var c = 0; c < rows[r].Length; c++)
            ws.Cell(r + 1, c + 1).Value = rows[r][c];
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<ImportBatch> ValidateUploadAsync(
        Stream stream,
        string fileName,
        string? contentType,
        ImportEntityKind kind,
        string userId,
        bool allowApprove,
        CancellationToken ct)
    {
        var rows = ReadRows(stream, fileName);
        var errors = new List<ImportRowError>();
        var preview = new List<Dictionary<string, string?>>();

        switch (kind)
        {
            case ImportEntityKind.Grades:
                ValidateGrades(rows, errors, preview);
                break;
            case ImportEntityKind.Rules:
                ValidateRules(rows, errors, preview);
                break;
            case ImportEntityKind.Sentences:
                await ValidateSentencesAsync(rows, errors, preview, allowApprove, ct);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var batch = new ImportBatch
        {
            UploadedByUserId = userId,
            FileName = fileName,
            ContentType = contentType,
            EntityKind = kind,
            Status = errors.Count == 0 ? ImportBatchStatus.Validated : ImportBatchStatus.Failed,
            RowCount = preview.Count,
            SuccessCount = preview.Count - errors.Select(e => e.RowNumber).Distinct().Count(),
            ErrorCount = errors.Select(e => e.RowNumber).Distinct().Count(),
            ErrorReportJson = JsonSerializer.Serialize(errors, JsonOpts),
            PreviewJson = JsonSerializer.Serialize(preview, JsonOpts),
            CreatedAtUtc = DateTime.UtcNow
        };

        // SuccessCount should be rows without any error
        var badRows = errors.Select(e => e.RowNumber).ToHashSet();
        batch.SuccessCount = preview.Count(p =>
        {
            var rn = int.Parse(p["__row"] ?? "0", CultureInfo.InvariantCulture);
            return !badRows.Contains(rn);
        });
        batch.ErrorCount = badRows.Count;

        _db.ImportBatches.Add(batch);
        await _db.SaveChangesAsync(ct);
        return batch;
    }

    public async Task<ImportBatch> CommitAsync(int batchId, string userId, bool allowApprove, CancellationToken ct)
    {
        var batch = await _db.ImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new InvalidOperationException("Import batch not found.");

        if (batch.Status != ImportBatchStatus.Validated)
            throw new InvalidOperationException("Only validated batches can be committed.");

        var preview = JsonSerializer.Deserialize<List<Dictionary<string, string?>>>(batch.PreviewJson ?? "[]", JsonOpts)
            ?? [];

        var committed = 0;
        switch (batch.EntityKind)
        {
            case ImportEntityKind.Grades:
                committed = await CommitGradesAsync(preview, userId, ct);
                break;
            case ImportEntityKind.Rules:
                committed = await CommitRulesAsync(preview, userId, ct);
                break;
            case ImportEntityKind.Sentences:
                committed = await CommitSentencesAsync(preview, userId, allowApprove, ct);
                break;
        }

        batch.Status = ImportBatchStatus.Committed;
        batch.SuccessCount = committed;
        batch.CommittedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return batch;
    }

    public static ImportBatchDto ToDto(ImportBatch batch)
    {
        var errors = string.IsNullOrEmpty(batch.ErrorReportJson)
            ? []
            : JsonSerializer.Deserialize<List<ImportRowError>>(batch.ErrorReportJson, JsonOpts) ?? [];
        var preview = string.IsNullOrEmpty(batch.PreviewJson)
            ? new List<IDictionary<string, string?>>()
            : (JsonSerializer.Deserialize<List<Dictionary<string, string?>>>(batch.PreviewJson, JsonOpts) ?? [])
                .Cast<IDictionary<string, string?>>()
                .ToList();

        return new ImportBatchDto(
            batch.Id,
            batch.FileName,
            batch.ContentType,
            (byte)batch.EntityKind,
            (byte)batch.Status,
            batch.RowCount,
            batch.SuccessCount,
            batch.ErrorCount,
            errors,
            preview,
            batch.CreatedAtUtc,
            batch.CommittedAtUtc);
    }

    private static List<Dictionary<string, string?>> ReadRows(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is ".xlsx" or ".xlsm")
            return ReadExcel(stream);
        return ReadCsv(stream);
    }

    private static List<Dictionary<string, string?>> ReadCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };
        using var csv = new CsvReader(reader, config);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        var list = new List<Dictionary<string, string?>>();
        var rowNum = 1;
        while (csv.Read())
        {
            rowNum++;
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["__row"] = rowNum.ToString(CultureInfo.InvariantCulture)
            };
            foreach (var h in headers)
            {
                if (string.IsNullOrWhiteSpace(h)) continue;
                dict[h.Trim()] = csv.GetField(h)?.Trim();
            }
            if (dict.Where(kv => kv.Key != "__row").All(kv => string.IsNullOrWhiteSpace(kv.Value)))
                continue;
            list.Add(dict);
        }
        return list;
    }

    private static List<Dictionary<string, string?>> ReadExcel(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var range = ws.RangeUsed();
        if (range is null) return [];
        var rows = range.RowsUsed().ToList();
        if (rows.Count < 2) return [];
        var headers = rows[0].Cells().Select(c => c.GetString().Trim()).ToList();
        var list = new List<Dictionary<string, string?>>();
        for (var i = 1; i < rows.Count; i++)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["__row"] = (i + 1).ToString(CultureInfo.InvariantCulture)
            };
            var cells = rows[i].Cells().ToList();
            for (var c = 0; c < headers.Count; c++)
            {
                if (string.IsNullOrWhiteSpace(headers[c])) continue;
                var val = c < cells.Count ? cells[c].GetString().Trim() : null;
                dict[headers[c]] = string.IsNullOrWhiteSpace(val) ? null : val;
            }
            if (dict.Where(kv => kv.Key != "__row").All(kv => string.IsNullOrWhiteSpace(kv.Value)))
                continue;
            list.Add(dict);
        }
        return list;
    }

    private static void ValidateGrades(
        List<Dictionary<string, string?>> rows,
        List<ImportRowError> errors,
        List<Dictionary<string, string?>> preview)
    {
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var rn = int.Parse(row["__row"]!, CultureInfo.InvariantCulture);
            var nameEn = Get(row, "NameEn");
            var code = Get(row, "Code");
            if (string.IsNullOrWhiteSpace(nameEn))
                errors.Add(new ImportRowError(rn, "NameEn", "NameEn is required."));
            if (!string.IsNullOrWhiteSpace(code) && !seenCodes.Add(code))
                errors.Add(new ImportRowError(rn, "Code", $"Duplicate Code '{code}' in file."));
            preview.Add(row);
        }
    }

    private static void ValidateRules(
        List<Dictionary<string, string?>> rows,
        List<ImportRowError> errors,
        List<Dictionary<string, string?>> preview)
    {
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var rn = int.Parse(row["__row"]!, CultureInfo.InvariantCulture);
            var nameEn = Get(row, "NameEn");
            var code = Get(row, "Code");
            var catName = Get(row, "CategoryNameEn");
            var catCode = Get(row, "CategoryCode");
            if (string.IsNullOrWhiteSpace(nameEn))
                errors.Add(new ImportRowError(rn, "NameEn", "NameEn is required."));
            if (string.IsNullOrWhiteSpace(code))
                errors.Add(new ImportRowError(rn, "Code", "Code is required for rule items."));
            else if (!seenCodes.Add(code))
                errors.Add(new ImportRowError(rn, "Code", $"Duplicate Code '{code}' in file."));
            if (string.IsNullOrWhiteSpace(catName) && string.IsNullOrWhiteSpace(catCode))
                errors.Add(new ImportRowError(rn, "CategoryNameEn", "CategoryNameEn or CategoryCode is required."));
            preview.Add(row);
        }
    }

    private async Task ValidateSentencesAsync(
        List<Dictionary<string, string?>> rows,
        List<ImportRowError> errors,
        List<Dictionary<string, string?>> preview,
        bool allowApprove,
        CancellationToken ct)
    {
        var grades = await _db.Grades.AsNoTracking().ToListAsync(ct);
        var rules = await _db.RuleItems.AsNoTracking().Where(r => r.Code != null).ToListAsync(ct);
        var gradeByCode = grades.Where(g => g.Code != null)
            .ToDictionary(g => g.Code!, g => g, StringComparer.OrdinalIgnoreCase);
        var gradeByName = grades.ToDictionary(g => g.NameEn, g => g, StringComparer.OrdinalIgnoreCase);
        var ruleByCode = rules.ToDictionary(r => r.Code!, r => r, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var rn = int.Parse(row["__row"]!, CultureInfo.InvariantCulture);
            var gradeKey = Get(row, "GradeCode") ?? Get(row, "Grade");
            var textEn = Get(row, "TextEn");
            var textUr = Get(row, "TextUr");
            var diff = Get(row, "Difficulty");
            var status = Get(row, "Status");
            var ruleCodes = Get(row, "RuleCodes");

            if (string.IsNullOrWhiteSpace(gradeKey))
                errors.Add(new ImportRowError(rn, "GradeCode", "GradeCode (or Grade name) is required."));
            else if (!gradeByCode.ContainsKey(gradeKey) && !gradeByName.ContainsKey(gradeKey))
                errors.Add(new ImportRowError(rn, "GradeCode", $"Unknown grade '{gradeKey}'."));

            if (string.IsNullOrWhiteSpace(textEn) && string.IsNullOrWhiteSpace(textUr))
                errors.Add(new ImportRowError(rn, "TextEn", "At least TextEn or TextUr is required."));

            if (!TryParseDifficulty(diff, out _))
                errors.Add(new ImportRowError(rn, "Difficulty", "Use Easy|Medium|Hard or 1|2|3."));

            if (!string.IsNullOrWhiteSpace(status) && !TryParseStatus(status, out var st))
                errors.Add(new ImportRowError(rn, "Status", "Use Draft or Approved."));
            else if (!string.IsNullOrWhiteSpace(status) && TryParseStatus(status, out var st2)
                     && st2 == SentenceStatus.Approved && !allowApprove)
                errors.Add(new ImportRowError(rn, "Status", "Only Admin/Teacher can import as Approved."));

            if (!string.IsNullOrWhiteSpace(ruleCodes))
            {
                foreach (var code in SplitCodes(ruleCodes))
                {
                    if (!ruleByCode.ContainsKey(code))
                        errors.Add(new ImportRowError(rn, "RuleCodes", $"Unknown rule code '{code}'."));
                }
            }

            preview.Add(row);
        }
    }

    private async Task<int> CommitGradesAsync(List<Dictionary<string, string?>> rows, string userId, CancellationToken ct)
    {
        var count = 0;
        foreach (var row in rows)
        {
            var code = Get(row, "Code");
            Grade? existing = null;
            if (!string.IsNullOrWhiteSpace(code))
                existing = await _db.Grades.FirstOrDefaultAsync(g => g.Code == code, ct);

            if (existing is null)
            {
                existing = new Grade
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                _db.Grades.Add(existing);
            }

            existing.Code = code;
            existing.NameEn = Get(row, "NameEn")!;
            existing.NameUr = Get(row, "NameUr");
            existing.SortOrder = int.TryParse(Get(row, "SortOrder"), out var so) ? so : 0;
            existing.IsActive = ParseBool(Get(row, "IsActive"), true);
            existing.UpdatedAtUtc = DateTime.UtcNow;
            count++;
        }
        await _db.SaveChangesAsync(ct);
        return count;
    }

    private async Task<int> CommitRulesAsync(List<Dictionary<string, string?>> rows, string userId, CancellationToken ct)
    {
        var count = 0;
        foreach (var row in rows)
        {
            var catCode = Get(row, "CategoryCode");
            var catNameEn = Get(row, "CategoryNameEn") ?? catCode ?? "General";
            RuleCategory? category = null;
            if (!string.IsNullOrWhiteSpace(catCode))
                category = await _db.RuleCategories.FirstOrDefaultAsync(c => c.Code == catCode, ct);
            if (category is null)
                category = await _db.RuleCategories.FirstOrDefaultAsync(c => c.NameEn == catNameEn, ct);
            if (category is null)
            {
                category = new RuleCategory
                {
                    Code = string.IsNullOrWhiteSpace(catCode) ? null : catCode.Trim(),
                    NameEn = catNameEn.Trim(),
                    NameUr = Get(row, "CategoryNameUr"),
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                _db.RuleCategories.Add(category);
                await _db.SaveChangesAsync(ct);
            }

            var code = Get(row, "Code")!;
            var existing = await _db.RuleItems.FirstOrDefaultAsync(r => r.Code == code, ct);
            if (existing is null)
            {
                existing = new RuleItem
                {
                    Code = code,
                    CategoryId = category.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                _db.RuleItems.Add(existing);
            }

            existing.CategoryId = category.Id;
            existing.NameEn = Get(row, "NameEn")!;
            existing.NameUr = Get(row, "NameUr");
            existing.ExplanationEn = Get(row, "ExplanationEn");
            existing.ExplanationUr = Get(row, "ExplanationUr");
            existing.IsActive = ParseBool(Get(row, "IsActive"), true);
            existing.UpdatedAtUtc = DateTime.UtcNow;
            count++;
        }
        await _db.SaveChangesAsync(ct);
        return count;
    }

    private async Task<int> CommitSentencesAsync(
        List<Dictionary<string, string?>> rows,
        string userId,
        bool allowApprove,
        CancellationToken ct)
    {
        var grades = await _db.Grades.ToListAsync(ct);
        var rules = await _db.RuleItems.Where(r => r.Code != null).ToListAsync(ct);
        var gradeByCode = grades.Where(g => g.Code != null)
            .ToDictionary(g => g.Code!, g => g, StringComparer.OrdinalIgnoreCase);
        var gradeByName = grades.ToDictionary(g => g.NameEn, g => g, StringComparer.OrdinalIgnoreCase);
        var ruleByCode = rules.ToDictionary(r => r.Code!, r => r, StringComparer.OrdinalIgnoreCase);

        var count = 0;
        foreach (var row in rows)
        {
            var gradeKey = Get(row, "GradeCode") ?? Get(row, "Grade")!;
            var grade = gradeByCode.GetValueOrDefault(gradeKey) ?? gradeByName[gradeKey];
            TryParseDifficulty(Get(row, "Difficulty"), out var difficulty);
            var status = SentenceStatus.Draft;
            if (!string.IsNullOrWhiteSpace(Get(row, "Status")) && TryParseStatus(Get(row, "Status"), out var st))
            {
                status = st == SentenceStatus.Approved && allowApprove ? SentenceStatus.Approved : SentenceStatus.Draft;
            }

            var textEn = Get(row, "TextEn") ?? string.Empty;
            var textUr = Get(row, "TextUr") ?? string.Empty;

            var sentence = new Sentence
            {
                TextEn = textEn,
                TextUr = textUr,
                GradeId = grade.Id,
                Difficulty = difficulty,
                Status = status,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = userId,
                ApprovedAtUtc = status == SentenceStatus.Approved ? DateTime.UtcNow : null,
                ApprovedByUserId = status == SentenceStatus.Approved ? userId : null
            };
            _db.Sentences.Add(sentence);
            await _db.SaveChangesAsync(ct);

            var codes = SplitCodes(Get(row, "RuleCodes"));
            foreach (var code in codes)
            {
                if (ruleByCode.TryGetValue(code, out var rule))
                {
                    _db.SentenceRules.Add(new SentenceRule { SentenceId = sentence.Id, RuleItemId = rule.Id });
                }
            }
            await _db.SaveChangesAsync(ct);
            count++;
        }
        return count;
    }

    private static string? Get(Dictionary<string, string?> row, string key) =>
        row.TryGetValue(key, out var v) ? v : null;

    private static IEnumerable<string> SplitCodes(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool ParseBool(string? raw, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        return raw.Equals("true", StringComparison.OrdinalIgnoreCase)
               || raw.Equals("1", StringComparison.OrdinalIgnoreCase)
               || raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseDifficulty(string? raw, out Difficulty difficulty)
    {
        difficulty = Difficulty.Easy;
        if (string.IsNullOrWhiteSpace(raw)) return true;
        if (byte.TryParse(raw, out var n) && Enum.IsDefined(typeof(Difficulty), n))
        {
            difficulty = (Difficulty)n;
            return true;
        }
        if (Enum.TryParse<Difficulty>(raw, ignoreCase: true, out difficulty))
            return true;
        return false;
    }

    private static bool TryParseStatus(string? raw, out SentenceStatus status)
    {
        status = SentenceStatus.Draft;
        if (string.IsNullOrWhiteSpace(raw)) return true;
        if (byte.TryParse(raw, out var n) && Enum.IsDefined(typeof(SentenceStatus), n))
        {
            status = (SentenceStatus)n;
            return true;
        }
        return Enum.TryParse(raw, ignoreCase: true, out status);
    }
}
