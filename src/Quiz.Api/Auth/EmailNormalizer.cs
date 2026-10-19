namespace Quiz.Api.Auth;

/// <summary>Une seule façon d'écrire une adresse e-mail en base : sans espaces, en minuscules.</summary>
public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
