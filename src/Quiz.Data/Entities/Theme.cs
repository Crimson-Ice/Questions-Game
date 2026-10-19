namespace Quiz.Data.Entities;

public class Theme
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Icon { get; set; }

    public List<Question> Questions { get; set; } = [];
}
