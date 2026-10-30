using EnUrGrammar.Api.Domain;
using EnUrGrammar.Api.Services;
using EnUrGrammar.Contracts.Practice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EnUrGrammar.Api.Controllers;

[ApiController]
[Route("api/saved-tests")]
[Authorize]
public class SavedTestsController : ControllerBase
{
    private readonly SavedTestService _savedTests;
    private readonly PracticeService _practice;
    private readonly UserManager<ApplicationUser> _users;

    public SavedTestsController(
        SavedTestService savedTests,
        PracticeService practice,
        UserManager<ApplicationUser> users)
    {
        _savedTests = savedTests;
        _practice = practice;
        _users = users;
    }

    /// <summary>List saved tests. Students see active only; authors see all.</summary>
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<ActionResult<IReadOnlyList<SavedTestSummaryDto>>> List(CancellationToken ct)
    {
        var activeOnly = !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Teacher);
        return Ok(await _savedTests.ListAsync(activeOnly, ct));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<ActionResult<SavedTestDto>> Get(int id, CancellationToken ct)
    {
        var dto = await _savedTests.GetAsync(id, ct);
        if (dto is null) return NotFound();
        var isAuthor = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Teacher);
        if (!dto.IsActive && !isAuthor) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
    public async Task<ActionResult<SavedTestDto>> Create(
        [FromBody] UpsertSavedTestRequest req, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Unauthorized();

        var (dto, error) = await _savedTests.CreateAsync(userId, req, ct);
        if (error is not null) return BadRequest(new { error });
        return CreatedAtAction(nameof(Get), new { id = dto!.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
    public async Task<ActionResult<SavedTestDto>> Update(
        int id, [FromBody] UpsertSavedTestRequest req, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Unauthorized();

        var (dto, error) = await _savedTests.UpdateAsync(id, userId, req, ct);
        if (error is "Saved test not found.") return NotFound(new { error });
        if (error is not null) return BadRequest(new { error });
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var (ok, error) = await _savedTests.DeactivateAsync(id, ct);
        if (!ok) return NotFound(new { error });
        return NoContent();
    }

    /// <summary>Start a practice session from a saved test definition.</summary>
    [HttpPost("{id:int}/start")]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<ActionResult<PracticeSessionDto>> Start(int id, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Unauthorized();

        var (dto, error) = await _practice.StartFromSavedTestAsync(userId, id, ct);
        if (error is "Saved test not found.") return NotFound(new { error });
        if (error is not null) return BadRequest(new { error });
        return Ok(dto);
    }
}
