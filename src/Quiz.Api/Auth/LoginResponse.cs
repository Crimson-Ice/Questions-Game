namespace Quiz.Api.Auth;

/// <summary>Jeton à envoyer dans l'en-tête <c>Authorization: Bearer ...</c>, et sa date d'expiration.</summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
