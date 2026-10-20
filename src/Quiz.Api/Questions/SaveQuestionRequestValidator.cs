using FluentValidation;

namespace Quiz.Api.Questions;

/// <summary>
/// Règles communes du contenu d'une question, plus le thème obligatoire.
/// L'existence du thème est vérifiée en base par l'endpoint.
/// </summary>
public sealed class SaveQuestionRequestValidator : QuestionContentValidator<SaveQuestionRequest>
{
    public SaveQuestionRequestValidator()
    {
        RuleFor(r => r.ThemeId).GreaterThan(0).WithMessage("Le thème est obligatoire.");
    }
}
