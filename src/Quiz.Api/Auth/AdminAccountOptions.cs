namespace Quiz.Api.Auth;

/// <summary>
/// Compte administrateur créé au démarrage s'il n'existe pas encore (section « Admin »).
/// Si l'e-mail n'est pas renseigné, aucun compte n'est créé.
/// </summary>
public sealed class AdminAccountOptions
{
    public const string SectionName = "Admin";

    public const int MinimumPasswordLength = 12;

    public string? Email { get; init; }

    public string? Password { get; init; }
}
