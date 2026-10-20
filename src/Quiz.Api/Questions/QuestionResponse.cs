using Quiz.Data.Entities;

namespace Quiz.Api.Questions;

/// <summary>Le détail d'une question dans le back-office, avec ses réponses.</summary>
public sealed record QuestionResponse(
    int Id,
    string Text,
    int ThemeId,
    string ThemeName,
    Difficulty? Difficulty,
    DateTimeOffset CreatedAt,
    List<AnswerResponse> Answers);
