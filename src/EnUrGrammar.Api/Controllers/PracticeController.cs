using EnUrGrammar.Api.Domain;
using EnUrGrammar.Api.Services;
using EnUrGrammar.Contracts.Practice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EnUrGrammar.Api.Controllers;

[ApiController]
[Route("api/practice")]
[Authorize(Roles = $"{AppRoles.Student},{AppRoles.Teacher},{AppRoles.Admin}")]
public class PracticeController : ControllerBase
{
    private readonly PracticeService _practice;
    private readonly UserManager<ApplicationUser> _users;

    public PracticeController(PracticeService practice, UserManager<ApplicationUser> users)
    {
        _practice = practice;
        _users = users;
    }

    /// <summary>Active grades/rules for practice setup (students; no draft content).</summary>
    [HttpGet("catalog")]
    public async Task<ActionResult<PracticeCatalogDto>> Catalog(CancellationToken ct) =>
        Ok(await _practice.GetCatalogAsync(ct));

    [HttpPost("sessions")]
    public async Task<ActionResult<PracticeSessionDto>> Start(
        [FromBody] StartPracticeSessionRequest req, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Unauthorized();

        var (dto, error) = await _practice.StartAsync(userId, req, ct);
        if (error is not null) return BadRequest(new { error });
        return Ok(dto);
    }

    [HttpPost("sessions/{id:int}/attempts")]
    public async Task<ActionResult<AttemptResultDto>> SubmitAttempt(
        int id, [FromBody] SubmitAttemptRequest req, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Unauthorized();

        var (dto, error) = await _practice.SubmitAttemptAsync(userId, id, req, ct);
        if (error is not null)
        {
            if (error is "Session not found.") return NotFound(new { error });
            return BadRequest(new { error });
        }
        return Ok(dto);
    }

    [HttpPost("sessions/{id:int}/complete")]
    public async Task<ActionResult<PracticeSessionDto>> Complete(int id, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Unauthorized();

        var (dto, error) = await _practice.CompleteAsync(userId, id, ct);
        if (error is not null) return NotFound(new { error });
        return Ok(dto);
    }
}
