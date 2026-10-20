using Quiz.Api.Questions;
using Quiz.Data.Entities;

namespace Quiz.Api.Import;

/// <summary>Une question dans un fichier d'import : son thème est donné par le bloc qui la contient.</summary>
public sealed record ImportQuestionRequest : IQuestionContent
{
    public string Text { get; init; } = "";

    public Difficulty? Difficulty { get; init; }

    public List<SaveAnswerRequest> Answers { get; init; } = [];
}
