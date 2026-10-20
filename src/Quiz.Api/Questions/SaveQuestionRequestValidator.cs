using FluentValidation;

namespace Quiz.Api.Questions;

/// <summary>
/// Règles du cadrage : un énoncé, de 2 à 4 réponses dont exactement une correcte.
/// L'existence du thème est vérifiée en base par l'endpoint.
/// </summary>
public sealed class SaveQuestionRequestValidator : AbstractValidator<SaveQuestionRequest>
{
    public const int MinAnswers = 2;

    public const int MaxAnswers = 4;

    public SaveQuestionRequestValidator()
    {
        RuleFor(r => r.Text).Must(text => !string.IsNullOrWhiteSpace(text))
            .WithMessage("L'énoncé de la question est obligatoire.");
        RuleFor(r => r.Text).MaximumLength(500);

        RuleFor(r => r.ThemeId).GreaterThan(0).WithMessage("Le thème est obligatoire.");

        RuleFor(r => r.Difficulty).IsInEnum();

        RuleFor(r => r.Answers)
            .Must(answers => answers?.Count is >= MinAnswers and <= MaxAnswers)
            .WithMessage($"Une question doit avoir entre {MinAnswers} et {MaxAnswers} réponses.");
        RuleFor(r => r.Answers)
            .Must(answers => answers?.Count(a => a?.IsCorrect == true) == 1)
            .WithMessage("Une question doit avoir exactement une bonne réponse.");

        // Le JSON "answers": [null, …] est accepté par le désérialiseur : on le refuse ici.
        RuleForEach(r => r.Answers).NotNull().WithMessage("Une réponse ne peut pas être vide.");
        RuleForEach(r => r.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.Text).Must(text => !string.IsNullOrWhiteSpace(text))
                .WithMessage("Le texte de la réponse est obligatoire.");
            answer.RuleFor(a => a.Text).MaximumLength(200);
        });
    }
}
