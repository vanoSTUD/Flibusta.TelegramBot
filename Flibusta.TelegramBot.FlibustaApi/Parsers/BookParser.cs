using Flibusta.TelegramBot.Core.Abstractions;
using Flibusta.TelegramBot.Core.Entities;
using Flibusta.TelegramBot.Core.ResultPattern;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Flibusta.TelegramBot.FlibustaApi.Parsers;

public class BookParser : IPageParser<Book>
{
    private readonly ILogger<BookParser> _logger;
	private readonly HtmlWeb _htmlWeb = new();

	public BookParser(ILogger<BookParser> logger)
	{
		_logger = logger;
	}

	public async Task<Result<Book>> ParseAsync(Uri bookPageUri, int page = default, int pageSize = default, CancellationToken cancellationToken = default)
    {
		try
		{
			cancellationToken.ThrowIfCancellationRequested();

			var document = await _htmlWeb.LoadFromWebAsync(bookPageUri.AbsoluteUri, cancellationToken);
			var bookPageNode = document.DocumentNode;
			var bookId = int.Parse(bookPageUri.Segments[^1]);

            if (bookPageNode.SelectSingleNode("//div[@id='main']/div[@id='mission']") != null)
            {
                return new Error($"Книги с Id '{bookId}' не нейдено");
            }

            var book = new Book()
            {
                Id = bookId,
                Title = GetTitle(bookPageNode),
                Authors = GetAuthors(bookPageNode),
                AdditionDate = GetAdditionDate(bookPageNode),
                Genres = GetGenres(bookPageNode),
                PublicationYear = GetPublicationYear(bookPageNode),
                Description = GetDescription(bookPageNode),
                PhotoUri = GetPhotoUri(bookPageNode, bookPageUri)
            };

            return book;
        }
        catch(Exception ex)
        {
           _logger.LogError("Exception in BookPageParser: {e}", ex.Message);

            return new Error("Не удалось получить информацию о книге");
        }
    }

    private static Uri? GetPhotoUri(HtmlNode bookPageNode, Uri bookPageUri)
    {
        try
        {
            var bookImgNode = bookPageNode.SelectSingleNode($"//*[@id=\"main\"]/div[3]/div[1]/div[2]/div[1]/img");
            var photoSrc = bookImgNode.Attributes["src"].Value;
            
            return new Uri($"{bookPageUri.Scheme}://{bookPageUri.Host}{photoSrc}");
        }
        catch
        {
            return null;
        }
    }

    private static List<Author> GetAuthors(HtmlNode bookPageNode)
    {
        try
        {
            return bookPageNode.SelectSingleNode($"//div[@class='book_img']/img").ChildNodes
                .Where(node => node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText))
                    .Select(item => new Author()
                    {
                        Id = int.Parse(string.Join("", item.Attributes["href"].Value[3..])),
                        Name = item.InnerText
                    }).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string? GetDescription(HtmlNode bookPageNode)
    {
        try
        {
            var paragraphs = bookPageNode.SelectNodes("//*[@class='b_biblio_book_annotation']/p");
            var description = string.Empty;

            if (paragraphs == null)
                return null;

            foreach (var paragraph in paragraphs)
            {
                description += paragraph.InnerText + '\n';
            }

            return description;
        }
        catch
        {
            return null;
        }
    }

    private static string GetTitle(HtmlNode bookPageNode)
    {
        return bookPageNode.SelectSingleNode($"//div[@class='b_biblio_book_top']/div/h1").InnerText;
    }

    private static string? GetPublicationYear(HtmlNode bookPageNode)
    {
        try
        {
            var node = bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'year_public')]//span[@class='row_content']");

            if (node == null)
                return null;

            return node.InnerText;
        }
        catch 
        {
            return null;
        }
    }

    private static DateTime? GetAdditionDate(HtmlNode bookPageNode)
    {
        try
        {
            var dateString = bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'date_add')]//span[@class='row_content']").InnerText;

            if (DateTime.TryParse(dateString, out var date))
                return date;
            else
                return null;
        }
        catch
        {
            return null;
        }
    }

    private static List<string> GetGenres(HtmlNode bookPageNode)
    {
        try
        {
            return bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'genre')]//span[@class='row_content']").ChildNodes
            .Where(node => node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText))
                .Select(node => node.InnerText).ToList();
        }
        catch
        {
            return [];
        }
    }
}

