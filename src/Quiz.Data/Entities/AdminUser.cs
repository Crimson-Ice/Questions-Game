namespace Quiz.Data.Entities;

public class AdminUser
{
    public int Id { get; set; }

    /// <summary>Adresse e-mail normalisée (sans espaces, en minuscules).</summary>
    public required string Email { get; set; }

    /// <summary>Mot de passe haché par <c>PasswordHasher</c> : jamais le mot de passe en clair.</summary>
    public required string PasswordHash { get; set; }
}
