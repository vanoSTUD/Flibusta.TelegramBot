namespace Flibusta.TelegramBot.Core.Entities;

public class Book
{
    public int? Id { get; set; } = default;
    public string Title { get; set; } = string.Empty;
    public List<Author> Authors { get; set; } = [];
    public List<string> Genres { get; set; } = [];
    public Uri? PhotoUri { get; set; } = default;
    public string? PublicationYear { get; set; } = default;
    public string? Description { get; set; } = default;
    public DateTime? AdditionDate { get; set; } = default;
    public List<DownloadLink> DownloadLinks { get; set; } = [];

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

public class DownloadLink
{
    public string Name { get; set; } = string.Empty;
    public Uri? Uri { get; set; } = default;
}
