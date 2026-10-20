namespace Quiz.Api.Questions;

/// <summary>
/// Paramètres d'URL de la liste des questions, regroupés par <c>[AsParameters]</c>.
/// Les valeurs par défaut du constructeur rendent chaque paramètre facultatif.
/// </summary>
public sealed record QuestionListQuery(int Page = 1, int PageSize = 20, string? Search = null, int? ThemeId = null);
