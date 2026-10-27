namespace EnUrGrammar.Contracts.Auth;

public sealed record MeResponse(
    string Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
