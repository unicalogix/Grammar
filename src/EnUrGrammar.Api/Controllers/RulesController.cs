using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Contracts.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Controllers;

/// <summary>
/// Flat rule-item endpoints (backward-compatible /api/rules) plus category hierarchy under /api/rule-categories.
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

    // ── Flat rule items (for sentence linking, filters, grid dropdowns) ──────

    [HttpGet("api/rules")]
    public async Task<ActionResult<IEnumerable<RuleDto>>> ListItems([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var q = _db.RuleItems.AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.GradeRules)
            .AsQueryable();
        if (activeOnly == true) q = q.Where(r => r.IsActive && r.Category.IsActive);
        var list = await q.OrderBy(r => r.Category.SortOrder).ThenBy(r => r.SortOrder).ThenBy(r => r.NameEn)
            .ToListAsync(ct);
        return Ok(list.Select(ToItemDto));
    }

    [HttpGet("api/rules/{id:int}")]
    public async Task<ActionResult<RuleDto>> GetItem(int id, CancellationToken ct)
    {
        var r = await _db.RuleItems.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.GradeRules)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null ? NotFound() : Ok(ToItemDto(r));
    }

    [HttpPost("api/rules")]
    public async Task<ActionResult<RuleDto>> CreateItem([FromBody] RuleUpsertRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });
        if (!await _db.RuleCategories.AnyAsync(c => c.Id == req.CategoryId, ct))
            return BadRequest(new { error = "CategoryId is invalid." });

        var userId = _users.GetUserId(User);
        var entity = new RuleItem
        {
            CategoryId = req.CategoryId,
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
        _db.RuleItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        var loaded = await _db.RuleItems.AsNoTracking()
            .Include(x => x.Category).Include(x => x.GradeRules)
            .FirstAsync(x => x.Id == entity.Id, ct);
        return CreatedAtAction(nameof(GetItem), new { id = entity.Id }, ToItemDto(loaded));
    }

    [HttpPut("api/rules/{id:int}")]
    public async Task<ActionResult<RuleDto>> UpdateItem(int id, [FromBody] RuleUpsertRequest req, CancellationToken ct)
    {
        var entity = await _db.RuleItems.Include(r => r.GradeRules).Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });
        if (!await _db.RuleCategories.AnyAsync(c => c.Id == req.CategoryId, ct))
            return BadRequest(new { error = "CategoryId is invalid." });

        entity.CategoryId = req.CategoryId;
        entity.NameEn = req.NameEn.Trim();
        entity.NameUr = req.NameUr?.Trim();
        entity.ExplanationEn = req.ExplanationEn?.Trim();
        entity.ExplanationUr = req.ExplanationUr?.Trim();
        entity.Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim();
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _db.Entry(entity).Reference(r => r.Category).LoadAsync(ct);
        return Ok(ToItemDto(entity));
    }

    [HttpDelete("api/rules/{id:int}")]
    public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
    {
        var entity = await _db.RuleItems.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Categories (hierarchical admin) ─────────────────────────────────────

    [HttpGet("api/rule-categories")]
    public async Task<ActionResult<IEnumerable<RuleCategoryDto>>> ListCategories(
        [FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var q = _db.RuleCategories.AsNoTracking().Include(c => c.Items).AsQueryable();
        if (activeOnly == true) q = q.Where(c => c.IsActive);
        var list = await q.OrderBy(c => c.SortOrder).ThenBy(c => c.NameEn).ToListAsync(ct);
        return Ok(list.Select(ToCategoryDto));
    }

    [HttpGet("api/rule-categories/{id:int}")]
    public async Task<ActionResult<RuleCategoryDto>> GetCategory(int id, CancellationToken ct)
    {
        var c = await _db.RuleCategories.AsNoTracking().Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? NotFound() : Ok(ToCategoryDto(c));
    }

    [HttpPost("api/rule-categories")]
    public async Task<ActionResult<RuleCategoryDto>> CreateCategory(
        [FromBody] RuleCategoryWithItemsUpsertRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });

        var userId = _users.GetUserId(User);
        var entity = new RuleCategory
        {
            NameEn = req.NameEn.Trim(),
            NameUr = req.NameUr?.Trim(),
            Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        foreach (var item in req.Items ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.NameEn)) continue;
            entity.Items.Add(new RuleItem
            {
                NameEn = item.NameEn.Trim(),
                NameUr = item.NameUr?.Trim(),
                ExplanationEn = item.ExplanationEn?.Trim(),
                ExplanationUr = item.ExplanationUr?.Trim(),
                Code = string.IsNullOrWhiteSpace(item.Code) ? null : item.Code.Trim(),
                SortOrder = item.SortOrder,
                IsActive = item.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = userId
            });
        }

        _db.RuleCategories.Add(entity);
        await _db.SaveChangesAsync(ct);
        var loaded = await _db.RuleCategories.AsNoTracking().Include(c => c.Items)
            .FirstAsync(c => c.Id == entity.Id, ct);
        return CreatedAtAction(nameof(GetCategory), new { id = entity.Id }, ToCategoryDto(loaded));
    }

    [HttpPut("api/rule-categories/{id:int}")]
    public async Task<ActionResult<RuleCategoryDto>> UpdateCategory(
        int id, [FromBody] RuleCategoryWithItemsUpsertRequest req, CancellationToken ct)
    {
        var entity = await _db.RuleCategories.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);
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

        if (req.Items is not null)
        {
            var incomingIds = req.Items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
            foreach (var existing in entity.Items.Where(i => !incomingIds.Contains(i.Id)))
            {
                existing.IsActive = false;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            foreach (var item in req.Items)
            {
                if (string.IsNullOrWhiteSpace(item.NameEn)) continue;
                RuleItem target;
                if (item.Id is > 0)
                {
                    target = entity.Items.FirstOrDefault(i => i.Id == item.Id)
                             ?? new RuleItem { CategoryId = id, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId };
                    if (target.Id == 0) entity.Items.Add(target);
                }
                else
                {
                    target = new RuleItem
                    {
                        CategoryId = id,
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = userId
                    };
                    entity.Items.Add(target);
                }

                target.NameEn = item.NameEn.Trim();
                target.NameUr = item.NameUr?.Trim();
                target.ExplanationEn = item.ExplanationEn?.Trim();
                target.ExplanationUr = item.ExplanationUr?.Trim();
                target.Code = string.IsNullOrWhiteSpace(item.Code) ? null : item.Code.Trim();
                target.SortOrder = item.SortOrder;
                target.IsActive = item.IsActive;
                target.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        var loaded = await _db.RuleCategories.AsNoTracking().Include(c => c.Items)
            .FirstAsync(c => c.Id == id, ct);
        return Ok(ToCategoryDto(loaded));
    }

    [HttpDelete("api/rule-categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        var entity = await _db.RuleCategories.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        foreach (var item in entity.Items)
        {
            item.IsActive = false;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static RuleDto ToItemDto(RuleItem r) => new(
        r.Id,
        r.CategoryId,
        r.Category?.NameEn ?? string.Empty,
        r.NameEn,
        r.NameUr,
        r.ExplanationEn,
        r.ExplanationUr,
        r.Code,
        r.IsActive,
        r.GradeRules?.Select(gr => gr.GradeId).ToList() ?? []);

    private static RuleCategoryDto ToCategoryDto(RuleCategory c) => new(
        c.Id,
        c.NameEn,
        c.NameUr,
        c.Code,
        c.SortOrder,
        c.IsActive,
        c.Items
            .OrderBy(i => i.SortOrder).ThenBy(i => i.NameEn)
            .Select(i => new RuleItemSummaryDto(i.Id, i.NameEn, i.NameUr, i.Code, i.SortOrder, i.IsActive))
            .ToList());
}
