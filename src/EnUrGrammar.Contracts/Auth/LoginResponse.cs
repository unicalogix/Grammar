namespace EnUrGrammar.Contracts.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserInfo User);

public sealed record UserInfo(
    string Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
