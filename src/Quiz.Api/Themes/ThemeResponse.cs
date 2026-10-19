namespace Quiz.Api.Themes;

/// <summary>Un thème tel qu'affiché dans le back-office, avec son nombre de questions.</summary>
public sealed record ThemeResponse(int Id, string Name, string? Icon, int QuestionCount);
