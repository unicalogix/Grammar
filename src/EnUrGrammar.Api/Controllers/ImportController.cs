using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Api.Services;
using EnUrGrammar.Contracts.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
public class ImportController : ControllerBase
{
    private readonly ImportService _import;
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ImportController(ImportService import, AppDbContext db, UserManager<ApplicationUser> users)
    {
        _import = import;
        _db = db;
        _users = users;
    }

    [HttpGet("templates/{entityKind}")]
    public IActionResult DownloadTemplate(string entityKind, [FromQuery] string format = "csv")
    {
        if (!TryParseKind(entityKind, out var kind))
            return BadRequest(new { error = "entityKind must be grades, rules, or sentences." });

        format = (format ?? "csv").ToLowerInvariant();
        if (format is "xlsx" or "excel")
        {
            var bytes = _import.BuildTemplateXlsx(kind);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"enur-{kind.ToString().ToLowerInvariant()}-template.xlsx");
        }

        var csv = _import.BuildTemplateCsv(kind);
        return File(csv, "text/csv; charset=utf-8", $"enur-{kind.ToString().ToLowerInvariant()}-template.csv");
    }

    [HttpPost("batches")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ImportBatchDto>> Upload(
        [FromForm] IFormFile file,
        [FromForm] string entityKind,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "file is required." });
        if (!TryParseKind(entityKind, out var kind))
            return BadRequest(new { error = "entityKind must be grades, rules, or sentences." });

        var userId = _users.GetUserId(User) ?? string.Empty;
        var allowApprove = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Teacher);

        await using var stream = file.OpenReadStream();
        var batch = await _import.ValidateUploadAsync(stream, file.FileName, file.ContentType, kind, userId, allowApprove, ct);
        return Ok(ImportService.ToDto(batch));
    }

    [HttpGet("batches/{id:int}")]
    public async Task<ActionResult<ImportBatchDto>> GetBatch(int id, CancellationToken ct)
    {
        var batch = await _db.ImportBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        return batch is null ? NotFound() : Ok(ImportService.ToDto(batch));
    }

    [HttpPost("batches/{id:int}/commit")]
    public async Task<ActionResult<ImportCommitResult>> Commit(int id, CancellationToken ct)
    {
        var userId = _users.GetUserId(User) ?? string.Empty;
        var allowApprove = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Teacher);
        try
        {
            var batch = await _import.CommitAsync(id, userId, allowApprove, ct);
            return Ok(new ImportCommitResult(batch.Id, batch.SuccessCount, (byte)batch.Status));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static bool TryParseKind(string raw, out ImportEntityKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return raw.Trim().ToLowerInvariant() switch
        {
            "grades" or "grade" or "1" => Assign(ImportEntityKind.Grades, out kind),
            "rules" or "rule" or "2" => Assign(ImportEntityKind.Rules, out kind),
            "sentences" or "sentence" or "3" => Assign(ImportEntityKind.Sentences, out kind),
            _ => false
        };
    }

    private static bool Assign(ImportEntityKind value, out ImportEntityKind kind)
    {
        kind = value;
        return true;
    }
}
