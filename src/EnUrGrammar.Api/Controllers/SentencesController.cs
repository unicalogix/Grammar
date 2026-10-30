using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Contracts.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Controllers;

[ApiController]
[Route("api/sentences")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
public class SentencesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public SentencesController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SentenceDto>>> List(
        [FromQuery] int? gradeId,
        [FromQuery] byte? status,
        [FromQuery] byte? difficulty,
        [FromQuery] int? ruleId,
        CancellationToken ct)
    {
        var q = _db.Sentences.AsNoTracking()
            .Include(s => s.Grade)
            .Include(s => s.SentenceRules).ThenInclude(sr => sr.RuleItem)
            .Where(s => !s.IsDeleted);

        if (gradeId is not null) q = q.Where(s => s.GradeId == gradeId);
        if (status is not null) q = q.Where(s => (byte)s.Status == status);
        if (difficulty is not null) q = q.Where(s => (byte)s.Difficulty == difficulty);
        if (ruleId is not null) q = q.Where(s => s.SentenceRules.Any(sr => sr.RuleItemId == ruleId));

        var list = await q.OrderByDescending(s => s.CreatedAtUtc).Take(500).ToListAsync(ct);
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SentenceDto>> Get(int id, CancellationToken ct)
    {
        var s = await QueryOne(id).FirstOrDefaultAsync(ct);
        return s is null ? NotFound() : Ok(ToDto(s));
    }

    [HttpPost]
    public async Task<ActionResult<SentenceDto>> Create([FromBody] SentenceUpsertRequest req, CancellationToken ct)
    {
        var err = ValidateUpsert(req);
        if (err is not null) return BadRequest(new { error = err });

        if (!await _db.Grades.AnyAsync(g => g.Id == req.GradeId, ct))
            return BadRequest(new { error = "Invalid GradeId." });

        var userId = _users.GetUserId(User);
        var status = req.Status.HasValue && Enum.IsDefined(typeof(SentenceStatus), req.Status.Value)
            ? (SentenceStatus)req.Status.Value
            : SentenceStatus.Draft;

        var entity = new Sentence
        {
            TextEn = req.TextEn.Trim(),
            TextUr = req.TextUr.Trim(),
            GradeId = req.GradeId,
            Difficulty = (Difficulty)req.Difficulty,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId,
            ApprovedAtUtc = status == SentenceStatus.Approved ? DateTime.UtcNow : null,
            ApprovedByUserId = status == SentenceStatus.Approved ? userId : null
        };
        _db.Sentences.Add(entity);
        await _db.SaveChangesAsync(ct);

        try
        {
            await ReplaceRulesAsync(entity.Id, req.RuleIds, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        var loaded = await QueryOne(entity.Id).FirstAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(loaded));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SentenceDto>> Update(int id, [FromBody] SentenceUpsertRequest req, CancellationToken ct)
    {
        var err = ValidateUpsert(req);
        if (err is not null) return BadRequest(new { error = err });

        var entity = await _db.Sentences.Include(s => s.SentenceRules)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (entity is null) return NotFound();
        if (!await _db.Grades.AnyAsync(g => g.Id == req.GradeId, ct))
            return BadRequest(new { error = "Invalid GradeId." });

        var userId = _users.GetUserId(User);
        entity.TextEn = req.TextEn.Trim();
        entity.TextUr = req.TextUr.Trim();
        entity.GradeId = req.GradeId;
        entity.Difficulty = (Difficulty)req.Difficulty;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (req.Status.HasValue && Enum.IsDefined(typeof(SentenceStatus), req.Status.Value))
        {
            var status = (SentenceStatus)req.Status.Value;
            if (status == SentenceStatus.Approved && entity.Status != SentenceStatus.Approved)
            {
                entity.ApprovedAtUtc = DateTime.UtcNow;
                entity.ApprovedByUserId = userId;
            }
            entity.Status = status;
        }

        await _db.SaveChangesAsync(ct);
        if (req.RuleIds is not null)
        {
            try
            {
                await ReplaceRulesAsync(id, req.RuleIds, ct);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        var loaded = await QueryOne(id).FirstAsync(ct);
        return Ok(ToDto(loaded));
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<SentenceDto>> SetApproval(int id, [FromBody] SentenceApproveRequest req, CancellationToken ct)
    {
        var entity = await _db.Sentences.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (entity is null) return NotFound();
        var userId = _users.GetUserId(User);
        if (req.Approve)
        {
            entity.Status = SentenceStatus.Approved;
            entity.ApprovedAtUtc = DateTime.UtcNow;
            entity.ApprovedByUserId = userId;
        }
        else
        {
            entity.Status = SentenceStatus.Draft;
            entity.ApprovedAtUtc = null;
            entity.ApprovedByUserId = null;
        }
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        var loaded = await QueryOne(id).FirstAsync(ct);
        return Ok(ToDto(loaded));
    }

    [HttpPut("{id:int}/rules")]
    public async Task<ActionResult<SentenceDto>> SetRules(int id, [FromBody] SetSentenceRulesRequest req, CancellationToken ct)
    {
        if (!await _db.Sentences.AnyAsync(s => s.Id == id && !s.IsDeleted, ct))
            return NotFound();
        await ReplaceRulesAsync(id, req.RuleIds, ct);
        var loaded = await QueryOne(id).FirstAsync(ct);
        return Ok(ToDto(loaded));
    }

    [HttpPost("{id:int}/accept-mt")]
    public async Task<ActionResult<SentenceDto>> AcceptMt(int id, CancellationToken ct)
    {
        var entity = await _db.Sentences.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (entity is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(entity.MtSuggestedUr))
            entity.TextUr = entity.MtSuggestedUr;
        if (!string.IsNullOrWhiteSpace(entity.MtSuggestedEn))
            entity.TextEn = entity.MtSuggestedEn;
        entity.MtConfirmedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        // Explicitly do NOT change Status
        await _db.SaveChangesAsync(ct);
        var loaded = await QueryOne(id).FirstAsync(ct);
        return Ok(ToDto(loaded));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Sentences.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private IQueryable<Sentence> QueryOne(int id) =>
        _db.Sentences.AsNoTracking()
            .Include(s => s.Grade)
            .Include(s => s.SentenceRules).ThenInclude(sr => sr.RuleItem)
            .Where(s => s.Id == id && !s.IsDeleted);

    private async Task ReplaceRulesAsync(int sentenceId, IReadOnlyList<int>? ruleIds, CancellationToken ct)
    {
        var ids = (ruleIds ?? []).Distinct().ToList();
        if (ids.Count > 0)
        {
            var valid = await _db.RuleItems.Where(r => ids.Contains(r.Id)).Select(r => r.Id).ToListAsync(ct);
            if (valid.Count != ids.Count)
                throw new InvalidOperationException("One or more ruleIds are invalid.");
        }

        var existing = await _db.SentenceRules.Where(sr => sr.SentenceId == sentenceId).ToListAsync(ct);
        _db.SentenceRules.RemoveRange(existing);
        foreach (var rid in ids)
            _db.SentenceRules.Add(new SentenceRule { SentenceId = sentenceId, RuleItemId = rid });
        await _db.SaveChangesAsync(ct);
    }

    private static string? ValidateUpsert(SentenceUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TextEn) && string.IsNullOrWhiteSpace(req.TextUr))
            return "TextEn or TextUr is required.";
        if (!Enum.IsDefined(typeof(Difficulty), req.Difficulty))
            return "Difficulty must be 1 (Easy), 2 (Medium), or 3 (Hard).";
        return null;
    }

    private static SentenceDto ToDto(Sentence s) => new(
        s.Id,
        s.TextEn,
        s.TextUr,
        s.GradeId,
        s.Grade?.NameEn ?? string.Empty,
        (byte)s.Difficulty,
        (byte)s.Status,
        s.MtSuggestedUr,
        s.MtSuggestedEn,
        s.MtConfirmedAtUtc,
        s.CreatedAtUtc,
        s.ApprovedAtUtc,
        s.SentenceRules?.Select(sr => sr.RuleItemId).ToList() ?? [],
        s.SentenceRules?.Select(sr => sr.RuleItem?.Code ?? sr.RuleItemId.ToString()).ToList() ?? []);
}
