using Quiz.Api.Questions;
using Quiz.Data.Entities;

namespace Quiz.Api.Tests.Questions;

/// <summary>Règles du back-office testées sans base de données ni serveur.</summary>
public class SaveQuestionRequestValidatorTests
{
    private readonly SaveQuestionRequestValidator _validator = new();

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Validate_WithTwoToFourAnswersAndOneCorrect_IsValid(int answerCount)
    {
        var result = _validator.Validate(Request(answerCount, correctCount: 1));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void Validate_WithAnswerCountOutsideTwoToFour_IsInvalid(int answerCount)
    {
        var result = _validator.Validate(Request(answerCount, correctCount: Math.Min(1, answerCount)));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveQuestionRequest.Answers));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Validate_WithoutExactlyOneCorrectAnswer_IsInvalid(int correctCount)
    {
        var result = _validator.Validate(Request(answerCount: 4, correctCount));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Une question doit avoir exactement une bonne réponse.");
    }

    [Fact]
    public void Validate_WithNullAnswers_IsInvalid()
    {
        var result = _validator.Validate(Request(answerCount: 2, correctCount: 1) with { Answers = null! });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveQuestionRequest.Answers));
    }

    [Fact]
    public void Validate_WithNullAnswer_IsInvalid()
    {
        var request = Request(answerCount: 2, correctCount: 1);
        request.Answers[1] = null!;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == "Answers[1]");
    }

    [Fact]
    public void Validate_WithUnknownDifficulty_IsInvalid()
    {
        var result = _validator.Validate(Request(answerCount: 2, correctCount: 1) with { Difficulty = (Difficulty)7 });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveQuestionRequest.Difficulty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankText_IsInvalid(string text)
    {
        var result = _validator.Validate(Request(answerCount: 2, correctCount: 1) with { Text = text });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveQuestionRequest.Text));
    }

    [Fact]
    public void Validate_WithTooLongText_IsInvalid()
    {
        var result = _validator.Validate(Request(answerCount: 2, correctCount: 1) with { Text = new string('a', 501) });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveQuestionRequest.Text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankAnswerText_IsInvalid(string text)
    {
        var request = Request(answerCount: 2, correctCount: 1);
        request.Answers[1] = request.Answers[1] with { Text = text };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == "Answers[1].Text");
    }

    [Fact]
    public void Validate_WithTooLongAnswerText_IsInvalid()
    {
        var request = Request(answerCount: 2, correctCount: 1);
        request.Answers[0] = request.Answers[0] with { Text = new string('a', 201) };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == "Answers[0].Text");
    }

    [Fact]
    public void Validate_WithoutTheme_IsInvalid()
    {
        var result = _validator.Validate(Request(answerCount: 2, correctCount: 1) with { ThemeId = 0 });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveQuestionRequest.ThemeId));
    }

    private static SaveQuestionRequest Request(int answerCount, int correctCount) => new()
    {
        Text = "Quelle est la capitale de l'Australie ?",
        ThemeId = 1,
        Answers = Enumerable.Range(0, answerCount)
            .Select(i => new SaveAnswerRequest { Text = $"Réponse {i}", IsCorrect = i < correctCount })
            .ToList(),
    };
}
