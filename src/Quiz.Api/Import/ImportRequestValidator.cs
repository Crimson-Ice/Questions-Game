using FluentValidation;
using Quiz.Api.Questions;

namespace Quiz.Api.Import;

/// <summary>
/// Valide tout le fichier avant d'écrire quoi que ce soit : chaque question suit les mêmes règles
/// que le formulaire (<see cref="QuestionContentValidator{T}"/>).
/// </summary>
public sealed class ImportRequestValidator : AbstractValidator<ImportRequest>
{
    public ImportRequestValidator()
    {
        RuleFor(r => r.Themes).NotEmpty().WithMessage("Le fichier ne contient aucun thème.");
        RuleFor(r => r.Themes)
            .Must(HaveDistinctNames)
            .WithMessage("Un même thème apparaît plusieurs fois dans le fichier.");

        RuleForEach(r => r.Themes).NotNull().WithMessage("Un thème ne peut pas être vide.");
        RuleForEach(r => r.Themes).ChildRules(theme =>
        {
            theme.RuleFor(t => t.Name).Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Le nom du thème est obligatoire.");
            theme.RuleFor(t => t.Name).MaximumLength(100);
            theme.RuleFor(t => t.Icon).MaximumLength(50);

            theme.RuleFor(t => t.Questions).NotNull().WithMessage("La liste des questions est obligatoire (même vide).");
            theme.RuleForEach(t => t.Questions).NotNull().WithMessage("Une question ne peut pas être vide.");
            theme.RuleForEach(t => t.Questions).SetValidator(new QuestionContentValidator<ImportQuestionRequest>());
        });
    }

    private static bool HaveDistinctNames(List<ImportThemeRequest>? themes)
    {
        var names = themes?.Where(t => t is not null).Select(t => t.Name?.Trim() ?? "").ToList() ?? [];
        return names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count;
    }
}
