namespace Quiz.Api.Auth;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
