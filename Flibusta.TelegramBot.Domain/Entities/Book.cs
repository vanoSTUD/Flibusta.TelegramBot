namespace Flibusta.TelegramBot.Core.Entities;

public class Book
{
    public int? Id { get; set; }
    public string Title { get; set; } = default!;
    public List<Author> Authors { get; set; } = [];
    public List<string> Genres { get; set; } = [];
    public Uri? PhotoUri { get; set; }
    public string? PublicationYear { get; set; }
    public string? Description { get; set; }
    public DateTime? AdditionDate { get; set; }

    public string GetAuthors()
    {
        string authors = string.Empty;

        for (int i = 0; i < Authors.Count; i++)
        {
            authors += Authors[i].Name;

            if ((i + 1) < Authors.Count)
                authors += ", ";
        }

        return authors;
    }

    public string GetGenres()
    {
        string genres = string.Empty;

        for (int i = 0; i < Genres.Count; i++)
        {
            genres += Genres[i];

            if ((i + 1) < Genres.Count)
                genres += ", ";
        }

        return genres;
    }
}
