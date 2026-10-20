using Quiz.Api.Import;
using Quiz.Api.Questions;

namespace Quiz.Api.Tests.Import;

public class ImportRequestValidatorTests
{
    private readonly ImportRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidFile_IsValid()
    {
        Assert.True(_validator.Validate(File(Theme("Cinéma", Question("Q1")), Theme("Sport"))).IsValid);
    }

    [Fact]
    public void Validate_WithoutThemes_IsInvalid()
    {
        var result = _validator.Validate(new ImportRequest());

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ImportRequest.Themes));
    }

    [Fact]
    public void Validate_WithSameThemeTwiceInAnotherCase_IsInvalid()
    {
        var result = _validator.Validate(File(Theme("Cinéma"), Theme(" CINÉMA ")));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Un même thème apparaît plusieurs fois dans le fichier.");
    }

    [Fact]
    public void Validate_WithBlankThemeName_IsInvalid()
    {
        var result = _validator.Validate(File(Theme("  ")));

        Assert.Contains(result.Errors, e => e.PropertyName == "Themes[0].Name");
    }

    [Fact]
    public void Validate_WithInvalidQuestion_PointsToTheFaultyQuestion()
    {
        var invalid = Question("Deux bonnes réponses") with
        {
            Answers = [new() { Text = "A", IsCorrect = true }, new() { Text = "B", IsCorrect = true }],
        };

        var result = _validator.Validate(File(Theme("Cinéma"), Theme("Sport", Question("Q1"), invalid)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Themes[1].Questions[1].Answers");
    }

    [Fact]
    public void Validate_WithNullQuestionOrTheme_IsInvalid()
    {
        var result = _validator.Validate(new ImportRequest { Themes = [null!, Theme("Sport", [null!])] });

        Assert.Contains(result.Errors, e => e.PropertyName == "Themes[0]");
        Assert.Contains(result.Errors, e => e.PropertyName == "Themes[1].Questions[0]");
    }

    [Fact]
    public void Validate_WithTooLongThemeNameOrIcon_IsInvalid()
    {
        var result = _validator.Validate(File(Theme(new string('a', 101)) with { Icon = new string('b', 51) }));

        Assert.Contains(result.Errors, e => e.PropertyName == "Themes[0].Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Themes[0].Icon");
    }

    [Fact]
    public void Validate_WithNullQuestionList_IsInvalid()
    {
        var result = _validator.Validate(File(Theme("Sport") with { Questions = null! }));

        Assert.Contains(result.Errors, e => e.PropertyName == "Themes[0].Questions");
    }

    private static ImportRequest File(params ImportThemeRequest[] themes) => new() { Themes = [.. themes] };

    private static ImportThemeRequest Theme(string name, params ImportQuestionRequest[] questions) =>
        new() { Name = name, Questions = [.. questions] };

    private static ImportQuestionRequest Question(string text) => new()
    {
        Text = text,
        Answers = [new SaveAnswerRequest { Text = "Oui", IsCorrect = true }, new SaveAnswerRequest { Text = "Non" }],
    };
}
