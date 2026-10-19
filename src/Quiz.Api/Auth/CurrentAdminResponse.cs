namespace Quiz.Api.Auth;

/// <summary>L'administrateur connecté, tel que décrit par son jeton.</summary>
public sealed record CurrentAdminResponse(string Email);
