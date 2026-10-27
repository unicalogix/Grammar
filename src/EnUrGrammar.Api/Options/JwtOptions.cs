namespace EnUrGrammar.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "EnUrGrammar";
    public string Audience { get; set; } = "EnUrGrammar.Web";
    /// <summary>Signing key — set via user-secrets or env Jwt__Key (min 32 chars).</summary>
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}
