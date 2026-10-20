namespace Quiz.Api.Import;

/// <summary>Un thème dans un fichier d'import, retrouvé par son nom ou créé s'il n'existe pas.</summary>
public sealed record ImportThemeRequest
{
    public string Name { get; init; } = "";

    public string? Icon { get; init; }

    public List<ImportQuestionRequest> Questions { get; init; } = [];
}
