using Microsoft.AspNetCore.Mvc;

namespace EnUrGrammar.Api.Models;

/// <summary>Multipart form for POST /api/import/batches (Swagger-friendly single [FromForm] model).</summary>
public sealed class ImportUploadForm
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;

    [FromForm(Name = "entityKind")]
    public string EntityKind { get; set; } = string.Empty;
}
