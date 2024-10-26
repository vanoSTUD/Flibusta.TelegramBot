namespace Flibusta.TelegramBot.Domain.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public List<Author> Authors { get; set; } = [];
    public List<string> Genres { get; set; } = [];
    public string? PublicationYear { get; set; }
    public DateTime? AdditionDate { get; set; }
}
