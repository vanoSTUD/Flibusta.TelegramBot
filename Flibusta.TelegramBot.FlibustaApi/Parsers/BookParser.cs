using Flibusta.TelegramBot.Core.Abstractions;
using Flibusta.TelegramBot.Core.Entities;
using Flibusta.TelegramBot.Core.ResultPattern;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Flibusta.TelegramBot.FlibustaApi.Parsers;

public class BookParser : IPageParser<Book>, IBookFileProvider
{
	private readonly HtmlWeb _htmlWeb = new();
    private readonly ILogger<BookParser> _logger;

	public BookParser(ILogger<BookParser> logger)
	{
		_logger = logger;
	}

    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    public async Task<Result<Book>> ParseAsync(Uri bookPageUri, int page = default, int pageSize = default, CancellationToken cancellationToken = default)
    {
		cancellationToken.ThrowIfCancellationRequested();
		
        try
		{
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
                PhotoUri = GetPhotoUri(bookPageNode, bookPageUri),
                DownloadLinks = GetDownloadLinks(bookPageNode, bookPageUri),
            };

            return book;
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
           _logger.LogError("Исключени в BookPageParser: {e}", ex.Message);
            
            return new Error("Не удалось получить информацию о книге");
        }
    }

    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    public async Task<Result<Uri>> GetFileUri(Uri bookUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _htmlWeb.LoadFromWebAsync(bookUri.AbsoluteUri, cancellationToken);
            var fileNode = document.DocumentNode.SelectSingleNode("//div[@class='p_load_progress_txt']/a");

            if (fileNode == null)
                return new Error("Не удалось получить файл");

            var href = fileNode.Attributes["href"].Value;
            return new Uri(href);
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключени в BookPageParser.GetFileUri(): {e}", ex.Message);

            return new Error("Не удалось получить файл");
        }
    }

    private List<DownloadLink> GetDownloadLinks(HtmlNode bookPageNode, Uri bookPageUri)
    {
        try
        {
            var downloadNodes = bookPageNode.SelectNodes($"//div[@class='b_download']/span[@class='link']");

            if (downloadNodes == null)
                return [];

            if (downloadNodes.Count == 0)
                return [];

            var downloadLinks = new List<DownloadLink>();

            foreach (var node in downloadNodes)
            {
                var onclickAttrValue = node.Attributes["onclick"].Value;
                var pathToDownload = onclickAttrValue.Replace("window.open('", "").Replace("', '_top');", "");
                var name = node.InnerText; 

                if (string.IsNullOrEmpty(pathToDownload) || string.IsNullOrEmpty(name))
                    continue;

                if (name == "EPUB")
                    continue;

                var link = new Uri($"{bookPageUri.Scheme}://{bookPageUri.Host}{pathToDownload}");

                downloadLinks.Add(new DownloadLink
                {
                    Name = name,
                    Uri = link
                });
            } 

            return downloadLinks;
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключение в BookParser.GetDownloadLinks(): {e}", ex);

            return [];
        }
    }

    private Uri? GetPhotoUri(HtmlNode bookPageNode, Uri bookPageUri)
    {
        try
        {
            var bookImgNode = bookPageNode.SelectSingleNode($"//div[@class='book_img']/img");
            var photoSrc = bookImgNode.Attributes["src"].Value;
            
            return new Uri($"{bookPageUri.Scheme}://{bookPageUri.Host}{photoSrc}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключение в BookParser.GetPhotoUri(): {e}", ex);

            return null;
        }
    }

    private List<Author> GetAuthors(HtmlNode bookPageNode)
    {
        try
        {
            var authorsNodes = bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'author')]//span[@class='row_content']").ChildNodes
                .Where(node => node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText));

            var authors = authorsNodes.Select(item => new Author()
                {
                    Id = int.Parse(string.Join("", item.Attributes["href"].Value[3..])),
                    Name = item.InnerText
                }).ToList();

            if (authors.Count == 0)
            {
                return
                [
                    new Author()
                    {
                        Name = "Автор не известен",
                        Id = 0
                    }
                ];
            }

            return authors;
                    
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключение в BookParser.GetAuthors(): {e}", ex);

            return [];
        }
    }

    private string? GetDescription(HtmlNode bookPageNode)
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
        catch (Exception ex)
        {
            _logger.LogError("Исключение в BookParser.GetDescription(): {e}", ex);

            return null;
        }
    }

    private string GetTitle(HtmlNode bookPageNode)
    {
        try
        {
            var bookTitleNode = bookPageNode.SelectSingleNode($"//div[@class='b_biblio_book_top']/div/h1");
            return bookTitleNode.InnerText;
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключение в BookParser.GetTitle(): {e}", ex);

            return string.Empty;
        }
    }

    private string? GetPublicationYear(HtmlNode bookPageNode)
    {
        try
        {
            var node = bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'year_public')]//span[@class='row_content']");

            if (node == null)
                return null;

            return node.InnerText;
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключение в BookParser.GetPublicationYear(): {e}", ex);

            return null;
        }
    }

    private DateTime? GetAdditionDate(HtmlNode bookPageNode)
    {
        try
        {
            var dateString = bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'date_add')]//span[@class='row_content']").InnerText;

            if (DateTime.TryParse(dateString, out var date))
                return date;
            else
                return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключени в BookParser.GetAdditionDate(): {e}", ex);

            return null;
        }
    }

    private List<string> GetGenres(HtmlNode bookPageNode)
    {
        try
        {
            return bookPageNode.SelectSingleNode($"//div[@class='book_desc']//div[contains(@class, 'genre')]//span[@class='row_content']").ChildNodes
            .Where(node => node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText))
                .Select(node => node.InnerText).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключени в BookParser.GetGenres(): {e}", ex);

            return [];
        }
    }
}

