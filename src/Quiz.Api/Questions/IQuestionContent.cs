using Quiz.Data.Entities;

namespace Quiz.Api.Questions;

/// <summary>
/// Le contenu d'une question tel que saisi par l'administrateur, commun au formulaire
/// et à l'import JSON, pour leur appliquer les mêmes règles de validation.
/// </summary>
public interface IQuestionContent
{
    string Text { get; }

    Difficulty? Difficulty { get; }

    List<SaveAnswerRequest> Answers { get; }
}
