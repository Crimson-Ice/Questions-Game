using FluentValidation;

namespace Quiz.Api.Questions;

public sealed class QuestionListQueryValidator : AbstractValidator<QuestionListQuery>
{
    public const int MaxPageSize = 100;

    public QuestionListQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(q => q.Search).MaximumLength(200);
    }
}
