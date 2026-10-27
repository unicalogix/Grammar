using EnUrGrammar.Api.Data;
using EnUrGrammar.Api.Domain;
using EnUrGrammar.Api.Services;
using EnUrGrammar.Contracts.Translation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Controllers;

[ApiController]
[Route("api/translation")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
public class TranslationController : ControllerBase
{
    private readonly TranslationStubService _mt;
    private readonly AppDbContext _db;

    public TranslationController(TranslationStubService mt, AppDbContext db)
    {
        _mt = mt;
        _db = db;
    }

    /// <summary>Returns a stub MT suggestion. Never changes Status / never auto-publishes.</summary>
    [HttpPost("suggest")]
    public ActionResult<SuggestTranslationResponse> Suggest([FromBody] SuggestTranslationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TextEn) && string.IsNullOrWhiteSpace(request.TextUr))
            return BadRequest(new { error = "Provide TextEn and/or TextUr." });

        return Ok(_mt.Suggest(request));
    }

    /// <summary>Store suggestion on a sentence without approving.</summary>
    [HttpPost("suggest/{sentenceId:int}")]
    public async Task<ActionResult<SuggestTranslationResponse>> SuggestForSentence(
        int sentenceId,
        [FromBody] SuggestTranslationRequest? request,
        CancellationToken ct)
    {
        var sentence = await _db.Sentences.FirstOrDefaultAsync(s => s.Id == sentenceId && !s.IsDeleted, ct);
        if (sentence is null) return NotFound();

        var req = request ?? new SuggestTranslationRequest(sentence.TextEn, sentence.TextUr, "ur");
        if (string.IsNullOrWhiteSpace(req.TextEn)) req = req with { TextEn = sentence.TextEn };
        if (string.IsNullOrWhiteSpace(req.TextUr)) req = req with { TextUr = sentence.TextUr };

        var result = _mt.Suggest(req);
        if (!string.IsNullOrWhiteSpace(result.SuggestedUr))
            sentence.MtSuggestedUr = result.SuggestedUr;
        if (!string.IsNullOrWhiteSpace(result.SuggestedEn))
            sentence.MtSuggestedEn = result.SuggestedEn;
        sentence.UpdatedAtUtc = DateTime.UtcNow;
        // Do not touch Status
        await _db.SaveChangesAsync(ct);
        return Ok(result);
    }
}
