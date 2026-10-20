using Quiz.Api.Questions;

namespace Quiz.Api.Tests.Questions;

public class QuestionListQueryValidatorTests
{
    private readonly QuestionListQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaults_IsValid()
    {
        Assert.True(_validator.Validate(new QuestionListQuery()).IsValid);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 100)]
    public void Validate_WithPageAndPageSizeInRange_IsValid(int page, int pageSize)
    {
        Assert.True(_validator.Validate(new QuestionListQuery(page, pageSize)).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithPageBelowOne_IsInvalid(int page)
    {
        var result = _validator.Validate(new QuestionListQuery(Page: page));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(QuestionListQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithPageSizeOutOfRange_IsInvalid(int pageSize)
    {
        var result = _validator.Validate(new QuestionListQuery(PageSize: pageSize));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(QuestionListQuery.PageSize));
    }

    [Fact]
    public void Validate_WithTooLongSearch_IsInvalid()
    {
        var result = _validator.Validate(new QuestionListQuery(Search: new string('a', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(QuestionListQuery.Search));
    }
}
