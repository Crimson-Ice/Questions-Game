using Quiz.Data.Entities;

namespace Quiz.Api.Questions;

/// <summary>Une ligne de la liste des questions du back-office, sans le détail des réponses.</summary>
public sealed record QuestionSummaryResponse(
    int Id,
    string Text,
    int ThemeId,
    string ThemeName,
    Difficulty? Difficulty,
    int AnswerCount,
    DateTimeOffset CreatedAt);
