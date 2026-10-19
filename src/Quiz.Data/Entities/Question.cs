namespace Quiz.Data.Entities;

public class Question
{
    public int Id { get; set; }

    public required string Text { get; set; }

    public Difficulty? Difficulty { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int ThemeId { get; set; }

    public Theme Theme { get; set; } = null!;

    public List<Answer> Answers { get; set; } = [];
}
