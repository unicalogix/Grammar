using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Contracts.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Controllers;

/// <summary>
/// Master Rules under /api/rules (with nested details) plus flat RuleDetails under /api/rule-details
/// for sentence linking, filters, and recognition dropdowns.
/// </summary>
[ApiController]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
public class RulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public RulesController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ── Master Rules (admin authoring) ──────────────────────────────────────

    [HttpGet("api/rules")]
    public async Task<ActionResult<IEnumerable<RuleDto>>> ListRules(
        [FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var q = _db.Rules.AsNoTracking().Include(r => r.Details).AsQueryable();
        if (activeOnly == true) q = q.Where(r => r.IsActive);
        var list = await q.OrderBy(r => r.SortOrder).ThenBy(r => r.NameEn).ToListAsync(ct);
        return Ok(list.Select(ToRuleDto));
    }

    [HttpGet("api/rules/{id:int}")]
    public async Task<ActionResult<RuleDto>> GetRule(int id, CancellationToken ct)
    {
        var r = await _db.Rules.AsNoTracking().Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null ? NotFound() : Ok(ToRuleDto(r));
    }

    [HttpPost("api/rules")]
    public async Task<ActionResult<RuleDto>> CreateRule(
        [FromBody] RuleWithDetailsUpsertRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });

        var userId = _users.GetUserId(User);
        var entity = new Rule
        {
            NameEn = req.NameEn.Trim(),
            NameUr = req.NameUr?.Trim(),
            Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        foreach (var detail in req.Details ?? [])
        {
            if (string.IsNullOrWhiteSpace(detail.NameEn)) continue;
            entity.Details.Add(new RuleDetail
            {
                NameEn = detail.NameEn.Trim(),
                NameUr = detail.NameUr?.Trim(),
                ExplanationEn = detail.ExplanationEn?.Trim(),
                ExplanationUr = detail.ExplanationUr?.Trim(),
                Code = string.IsNullOrWhiteSpace(detail.Code) ? null : detail.Code.Trim(),
                SortOrder = detail.SortOrder,
                IsActive = detail.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = userId
            });
        }

        _db.Rules.Add(entity);
        await _db.SaveChangesAsync(ct);
        var loaded = await _db.Rules.AsNoTracking().Include(r => r.Details)
            .FirstAsync(r => r.Id == entity.Id, ct);
        return CreatedAtAction(nameof(GetRule), new { id = entity.Id }, ToRuleDto(loaded));
    }

    [HttpPut("api/rules/{id:int}")]
    public async Task<ActionResult<RuleDto>> UpdateRule(
        int id, [FromBody] RuleWithDetailsUpsertRequest req, CancellationToken ct)
    {
        var entity = await _db.Rules.Include(r => r.Details).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });

        var userId = _users.GetUserId(User);
        entity.NameEn = req.NameEn.Trim();
        entity.NameUr = req.NameUr?.Trim();
        entity.Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim();
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (req.Details is not null)
        {
            var incomingIds = req.Details.Where(d => d.Id is > 0).Select(d => d.Id!.Value).ToHashSet();
            foreach (var existing in entity.Details.Where(d => !incomingIds.Contains(d.Id)))
            {
                existing.IsActive = false;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            foreach (var detail in req.Details)
            {
                if (string.IsNullOrWhiteSpace(detail.NameEn)) continue;
                RuleDetail target;
                if (detail.Id is > 0)
                {
                    target = entity.Details.FirstOrDefault(d => d.Id == detail.Id)
                             ?? new RuleDetail { RuleId = id, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId };
                    if (target.Id == 0) entity.Details.Add(target);
                }
                else
                {
                    target = new RuleDetail
                    {
                        RuleId = id,
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = userId
                    };
                    entity.Details.Add(target);
                }

                target.NameEn = detail.NameEn.Trim();
                target.NameUr = detail.NameUr?.Trim();
                target.ExplanationEn = detail.ExplanationEn?.Trim();
                target.ExplanationUr = detail.ExplanationUr?.Trim();
                target.Code = string.IsNullOrWhiteSpace(detail.Code) ? null : detail.Code.Trim();
                target.SortOrder = detail.SortOrder;
                target.IsActive = detail.IsActive;
                target.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        var loaded = await _db.Rules.AsNoTracking().Include(r => r.Details)
            .FirstAsync(r => r.Id == id, ct);
        return Ok(ToRuleDto(loaded));
    }

    [HttpDelete("api/rules/{id:int}")]
    public async Task<IActionResult> DeleteRule(int id, CancellationToken ct)
    {
        var entity = await _db.Rules.Include(r => r.Details).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        foreach (var detail in entity.Details)
        {
            detail.IsActive = false;
            detail.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Flat RuleDetails (sentence linking, filters, grid dropdowns) ────────

    [HttpGet("api/rule-details")]
    public async Task<ActionResult<IEnumerable<RuleDetailDto>>> ListDetails(
        [FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var q = _db.RuleDetails.AsNoTracking()
            .Include(d => d.Rule)
            .Include(d => d.GradeRules)
            .AsQueryable();
        if (activeOnly == true) q = q.Where(d => d.IsActive && d.Rule.IsActive);
        var list = await q.OrderBy(d => d.Rule.SortOrder).ThenBy(d => d.SortOrder).ThenBy(d => d.NameEn)
            .ToListAsync(ct);
        return Ok(list.Select(ToDetailDto));
    }

    [HttpGet("api/rule-details/{id:int}")]
    public async Task<ActionResult<RuleDetailDto>> GetDetail(int id, CancellationToken ct)
    {
        var d = await _db.RuleDetails.AsNoTracking()
            .Include(x => x.Rule)
            .Include(x => x.GradeRules)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return d is null ? NotFound() : Ok(ToDetailDto(d));
    }

    [HttpPost("api/rule-details")]
    public async Task<ActionResult<RuleDetailDto>> CreateDetail(
        [FromBody] RuleDetailUpsertRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });
        if (!await _db.Rules.AnyAsync(r => r.Id == req.RuleId, ct))
            return BadRequest(new { error = "RuleId is invalid." });

        var userId = _users.GetUserId(User);
        var entity = new RuleDetail
        {
            RuleId = req.RuleId,
            NameEn = req.NameEn.Trim(),
            NameUr = req.NameUr?.Trim(),
            ExplanationEn = req.ExplanationEn?.Trim(),
            ExplanationUr = req.ExplanationUr?.Trim(),
            Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };
        _db.RuleDetails.Add(entity);
        await _db.SaveChangesAsync(ct);
        var loaded = await _db.RuleDetails.AsNoTracking()
            .Include(x => x.Rule).Include(x => x.GradeRules)
            .FirstAsync(x => x.Id == entity.Id, ct);
        return CreatedAtAction(nameof(GetDetail), new { id = entity.Id }, ToDetailDto(loaded));
    }

    [HttpPut("api/rule-details/{id:int}")]
    public async Task<ActionResult<RuleDetailDto>> UpdateDetail(
        int id, [FromBody] RuleDetailUpsertRequest req, CancellationToken ct)
    {
        var entity = await _db.RuleDetails.Include(d => d.GradeRules).Include(d => d.Rule)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });
        if (!await _db.Rules.AnyAsync(r => r.Id == req.RuleId, ct))
            return BadRequest(new { error = "RuleId is invalid." });

        entity.RuleId = req.RuleId;
        entity.NameEn = req.NameEn.Trim();
        entity.NameUr = req.NameUr?.Trim();
        entity.ExplanationEn = req.ExplanationEn?.Trim();
        entity.ExplanationUr = req.ExplanationUr?.Trim();
        entity.Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim();
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _db.Entry(entity).Reference(d => d.Rule).LoadAsync(ct);
        return Ok(ToDetailDto(entity));
    }

    [HttpDelete("api/rule-details/{id:int}")]
    public async Task<IActionResult> DeleteDetail(int id, CancellationToken ct)
    {
        var entity = await _db.RuleDetails.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static RuleDto ToRuleDto(Rule r) => new(
        r.Id,
        r.NameEn,
        r.NameUr,
        r.Code,
        r.SortOrder,
        r.IsActive,
        r.Details
            .OrderBy(d => d.SortOrder).ThenBy(d => d.NameEn)
            .Select(d => new RuleDetailSummaryDto(d.Id, d.NameEn, d.NameUr, d.Code, d.SortOrder, d.IsActive))
            .ToList());

    private static RuleDetailDto ToDetailDto(RuleDetail d) => new(
        d.Id,
        d.RuleId,
        d.Rule?.NameEn ?? string.Empty,
        d.NameEn,
        d.NameUr,
        d.ExplanationEn,
        d.ExplanationUr,
        d.Code,
        d.IsActive,
        d.GradeRules?.Select(gr => gr.GradeId).ToList() ?? []);
}
