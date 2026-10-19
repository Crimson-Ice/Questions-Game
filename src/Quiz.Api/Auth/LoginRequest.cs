namespace Quiz.Api.Auth;

/// <summary>Identifiants envoyés par le formulaire de connexion du back-office.</summary>
public sealed record LoginRequest
{
    public string Email { get; init; } = "";

    public string Password { get; init; } = "";
}
