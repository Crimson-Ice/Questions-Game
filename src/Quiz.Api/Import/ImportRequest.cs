namespace Quiz.Api.Import;

/// <summary>
/// Corps de l'import JSON, au même format que les fichiers de la banque initiale (<c>seed/</c>).
/// </summary>
public sealed record ImportRequest
{
    public List<ImportThemeRequest> Themes { get; init; } = [];
}
