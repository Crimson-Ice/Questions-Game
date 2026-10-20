namespace Quiz.Api.Questions;

/// <summary>Une réponse dans le corps de création ou de modification d'une question.</summary>
public sealed record SaveAnswerRequest
{
    public string Text { get; init; } = "";

    public bool IsCorrect { get; init; }
}
