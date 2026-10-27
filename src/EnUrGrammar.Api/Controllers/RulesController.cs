using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Contracts.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Controllers;

[ApiController]
[Route("api/rules")]
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RuleDto>>> List([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var q = _db.Rules.AsNoTracking().Include(r => r.GradeRules).AsQueryable();
        if (activeOnly == true) q = q.Where(r => r.IsActive);
        var list = await q.OrderBy(r => r.NameEn).ToListAsync(ct);
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RuleDto>> Get(int id, CancellationToken ct)
    {
        var r = await _db.Rules.AsNoTracking().Include(x => x.GradeRules)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null ? NotFound() : Ok(ToDto(r));
    }

    [HttpPost]
    public async Task<ActionResult<RuleDto>> Create([FromBody] RuleUpsertRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });

        var userId = _users.GetUserId(User);
        var entity = new Rule
        {
            NameEn = req.NameEn.Trim(),
            NameUr = req.NameUr?.Trim(),
            ExplanationEn = req.ExplanationEn?.Trim(),
            ExplanationUr = req.ExplanationUr?.Trim(),
            Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim(),
            IsActive = req.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };
        _db.Rules.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RuleDto>> Update(int id, [FromBody] RuleUpsertRequest req, CancellationToken ct)
    {
        var entity = await _db.Rules.Include(r => r.GradeRules).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.NameEn))
            return BadRequest(new { error = "NameEn is required." });

        entity.NameEn = req.NameEn.Trim();
        entity.NameUr = req.NameUr?.Trim();
        entity.ExplanationEn = req.ExplanationEn?.Trim();
        entity.ExplanationUr = req.ExplanationUr?.Trim();
        entity.Code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code.Trim();
        entity.IsActive = req.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Rules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static RuleDto ToDto(Rule r) => new(
        r.Id, r.NameEn, r.NameUr, r.ExplanationEn, r.ExplanationUr, r.Code, r.IsActive,
        r.GradeRules?.Select(gr => gr.GradeId).ToList() ?? []);
}
