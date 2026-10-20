using FluentValidation;

namespace Quiz.Api.Questions;

public sealed class QuestionListQueryValidator : AbstractValidator<QuestionListQuery>
{
    public const int MaxPageSize = 100;

    /// <summary>
    /// Borne haute de la page : le décalage (Page - 1) * PageSize doit tenir dans un int,
    /// sinon il devient négatif et PostgreSQL refuse la requête.
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public const int MaxSearchLength = 200;

    public QuestionListQueryValidator()
    {
        RuleFor(q => q.Page).InclusiveBetween(1, MaxPage);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(q => q.Search).MaximumLength(MaxSearchLength);
    }
}
