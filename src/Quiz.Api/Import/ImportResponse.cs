namespace Quiz.Api.Import;

/// <summary>Bilan d'un import : thèmes créés, questions ajoutées, questions ignorées car déjà présentes.</summary>
public sealed record ImportResponse(int ThemesCreated, int QuestionsImported, int QuestionsSkipped);
