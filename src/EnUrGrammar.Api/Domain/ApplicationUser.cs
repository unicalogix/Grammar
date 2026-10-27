using Microsoft.AspNetCore.Identity;

namespace EnUrGrammar.Api.Domain;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
