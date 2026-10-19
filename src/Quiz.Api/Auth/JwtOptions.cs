using System.ComponentModel.DataAnnotations;

namespace Quiz.Api.Auth;

/// <summary>Réglages des jetons JWT, lus dans la section « Jwt » de la configuration.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "";

    [Required]
    public string Audience { get; init; } = "";

    /// <summary>Clé secrète HMAC-SHA256 : au moins 32 caractères, jamais commitée.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = "";

    [Range(1, 24 * 60)]
    public int LifetimeMinutes { get; init; } = 60;
}
