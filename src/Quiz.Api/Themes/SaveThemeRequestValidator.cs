using FluentValidation;

namespace Quiz.Api.Themes;

public sealed class SaveThemeRequestValidator : AbstractValidator<SaveThemeRequest>
{
    public SaveThemeRequestValidator()
    {
        RuleFor(r => r.Name).Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Le nom du thème est obligatoire.");
        RuleFor(r => r.Name).MaximumLength(100);
        RuleFor(r => r.Icon).MaximumLength(50);
    }
}
