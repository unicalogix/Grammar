using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Contracts.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Controllers;

[ApiController]
[Route("api/grades")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
public class GradesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public GradesController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GradeDto>>> List([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var q = _db.Grades.AsNoTracking().Include(g => g.GradeRules).AsQueryable();
        if (activeOnly == true) q = q.Where(g => g.IsActive);
        var list = await q.OrderBy(g => g.SortOrder).ThenBy(g => g.NameEn).ToListAsync(ct);
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GradeDto>> Get(int id, CancellationToken ct)
    {
        var g = await _db.Grades.AsNoTracking().Include(x => x.GradeRules)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return g is null ? NotFound() : Ok(ToDto(g));
    }

    [HttpPost]
    public async Task<ActionResult<GradeDto>> Create([FromBody] GradeUpsertRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });

        var userId = _users.GetUserId(User);
        var entity = new Grade
        {
            NameEn = req.NameEn.Trim(),
            NameUr = req.NameUr?.Trim(),
            Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };
        _db.Grades.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GradeDto>> Update(int id, [FromBody] GradeUpsertRequest req, CancellationToken ct)
    {
        var entity = await _db.Grades.Include(g => g.GradeRules).FirstOrDefaultAsync(g => g.Id == id, ct);
        if (entity is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });

        entity.NameEn = req.NameEn.Trim();
        entity.NameUr = req.NameUr?.Trim();
        entity.Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim();
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Grades.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("{id:int}/rules")]
    public async Task<ActionResult<GradeDto>> SetRules(int id, [FromBody] SetGradeRulesRequest req, CancellationToken ct)
    {
        var entity = await _db.Grades.Include(g => g.GradeRules).FirstOrDefaultAsync(g => g.Id == id, ct);
        if (entity is null) return NotFound();

        var ruleIds = (req.RuleIds ?? []).Distinct().ToList();
        var existingRules = await _db.RuleDetails.Where(r => ruleIds.Contains(r.Id)).Select(r => r.Id).ToListAsync(ct);
        if (existingRules.Count != ruleIds.Count)
            return BadRequest(new { error = "One or more ruleIds are invalid." });

        _db.GradeRules.RemoveRange(entity.GradeRules);
        foreach (var rid in ruleIds)
            entity.GradeRules.Add(new GradeRule { GradeId = id, RuleDetailId = rid });

        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(entity));
    }

    private static GradeDto ToDto(Grade g) => new(
        g.Id, g.NameEn, g.NameUr, g.Code, g.SortOrder, g.IsActive,
        g.GradeRules?.Select(gr => gr.RuleDetailId).ToList() ?? []);
}
