namespace Quiz.Api.Themes;

/// <summary>Corps de la création (POST) et de la modification (PUT) d'un thème.</summary>
public sealed record SaveThemeRequest
{
    public string Name { get; init; } = "";

    public string? Icon { get; init; }
}
