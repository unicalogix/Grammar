namespace EnUrGrammar.Contracts.Health;

public sealed record PingResponse(
    string Message,
    DateTime UtcNow,
    string UserId,
    IReadOnlyList<string> Roles);
