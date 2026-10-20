using Quiz.Data.Entities;

namespace Quiz.Api.Questions;

/// <summary>
/// Corps de la création (POST) et de la modification (PUT) d'une question.
/// En modification, la liste des réponses remplace entièrement les réponses existantes.
/// </summary>
public sealed record SaveQuestionRequest
{
    public string Text { get; init; } = "";

    public int ThemeId { get; init; }

    public Difficulty? Difficulty { get; init; }

    public List<SaveAnswerRequest> Answers { get; init; } = [];
}
