using Flibusta.TelegramBot.Core.Abstractions;
using Flibusta.TelegramBot.Core.Entities;
using Flibusta.TelegramBot.Core.ResultPattern;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Web;

namespace Flibusta.TelegramBot.FlibustaApi.Parsers;

public class BookCollectionParser : IPageParser<List<Book>>, IBookCountProvider
{
	private readonly ILogger<BookCollectionParser> _logger;
	private readonly HtmlWeb _htmlWeb = new();

	public BookCollectionParser(ILogger<BookCollectionParser> logger)
	{
		_logger = logger;
	}

    /// <summary>
    /// Возвращает коллекцию книг согласно указанной пагинации 
    /// </summary>
    /// <param name="pageUri">Ссылка на веб-страницу поиска книги с get параметром ask</param>
    /// <param name="page">Пагинация: номер страницы</param>
    /// <param name="pageSize">Пагинация: кол-во книг в странице</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    public async Task<Result<List<Book>>> ParseAsync(Uri pageUri, int page, int pageSize,  CancellationToken cancellationToken = default)
	{
        cancellationToken.ThrowIfCancellationRequested();

        var queryParams = HttpUtility.ParseQueryString(pageUri.Query);
        var bookTitle = queryParams["ask"];

        try
        {
            var bookCountResult = await GetBookCountAsync(pageUri, cancellationToken);

            if (bookCountResult.IsFailure)
                return new Error($"Книг с совпадением '{bookTitle}' не найдено");

            var bookCount = bookCountResult.Value!;

            if (bookCount == 0)
                return new Error($"Книг с совпадением '{bookTitle}' не найдено");

            if ((page * pageSize - pageSize) >= bookCount)
                return new Error($"Похожих книг больше не найдено");

            var books = await GetBooksAsync(pageUri, page, pageSize, bookCount, cancellationToken);

            if (books == null)
                return new Error($"Не удалось получить информацию о книгах с совпадением '{bookTitle}'");

            if (books.Count == 0)
                return new Error($"Книг с совпадением '{bookTitle}' не найдено");

            return books;
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
			_logger.LogError("Exception: {e}", ex);

			return new Error($"Не удалось получить информацию о книгах с совпадением '{bookTitle}'");
        }
    }

    /// <summary>
    /// Возвращает кол-во найденных книг по веб-ссылке на страницу поиска книги с get параметром'ask'
    /// </summary>
    /// <param name="pageUri">Веб-ссылка на страницу посика книги с get параметром 'ask'</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    public async Task<Result<int>> GetBookCountAsync(Uri pageUri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            int bookCount = 0;
            var webPageCount = await GetWebpageCountAsync(pageUri, cancellationToken);

            for (int webPage = 0; webPage <= webPageCount; webPage++)
            {
                var bookCountByWebpage = await GetBookCountByWebpageAsync(pageUri, webPage, cancellationToken);

                bookCount += bookCountByWebpage;
            }

            return bookCount;
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
            _logger.LogError("Exception: {e}", ex);

            return new Error($"Не удалось получить информацию о книгах");
        }
    }

    private async Task<List<Book>> GetBooksAsync(Uri pageUri, int page, int pageSize, int bookCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var endBookNumber = page * pageSize;
            var startBookNumber = endBookNumber - pageSize + 1;
            var currentBookNumber = 0;
            var currentWebpageNumber = 0;
            var webPageCount = await GetWebpageCountAsync(pageUri, cancellationToken);
            var bookNodes = new List<HtmlNode>();

            while (currentBookNumber <= bookCount &&
                   currentBookNumber < endBookNumber)
            {
                var bookCountByWebpage = await GetBookCountByWebpageAsync(pageUri, currentWebpageNumber, cancellationToken);

                if (bookCountByWebpage == 0)
                    break;

                if ((bookCountByWebpage + currentBookNumber) <= startBookNumber)
                {
                    currentBookNumber += bookCountByWebpage;
                }
                else
                {
                    var skipBookCount = startBookNumber - currentBookNumber - 1;
                    var takeBookCount = pageSize;

                    if (skipBookCount < 0)
                    {
                        takeBookCount = pageSize + skipBookCount;
                        skipBookCount = 0;
                    }
                    else if (skipBookCount == startBookNumber)
                    {
                        skipBookCount--;
                    }

                    var queryParams = HttpUtility.ParseQueryString(pageUri.Query);
                    var requestUrl = $"{pageUri.Scheme}://{pageUri.Host}/booksearch?page={currentWebpageNumber}&ask={queryParams["ask"]}";
                    var document = await _htmlWeb.LoadFromWebAsync(requestUrl);

                    var newBookNodes = document.DocumentNode.SelectNodes("//div[@id='main']/li")
                            .Where(x => x.OuterHtml.Contains("/b/"))
                            .Skip(skipBookCount)
                            .Take(takeBookCount).ToList();

                    if (newBookNodes == null)
                        break;

                    if (newBookNodes.Count == 0)
                        break;

                    bookNodes.AddRange(newBookNodes);
                    currentBookNumber += newBookNodes.Count + skipBookCount;
                }

                currentWebpageNumber++;

                if (currentWebpageNumber > webPageCount)
                    break;
            }

            var books = new List<Book>();

            foreach (var bookNode in bookNodes)
            {
                var title = bookNode.FirstChild.InnerText;
                var authorName = bookNode.ChildNodes.Where(x => x.OuterHtml.Contains("/a/")).FirstOrDefault()?.InnerText ?? string.Empty;
                (var bookId, var authorId) = GetIds(bookNode);

                var book = new Book()
                {
                    Id = bookId,
                    Title = title,
                    Authors =
                    [
                        new Author()
                        {
                            Id = authorId,
                            Name = authorName
                        }
                    ]
                };

                books.Add(book);
            }

            return books;
        }
        catch(TaskCanceledException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключение при попытке получении книг в BookCollectionParser.GetBooksAsync(): {ex}", ex);
            return [];
        }
    }

	private async Task<int> GetBookCountByWebpageAsync(Uri pageUri, int webPage, CancellationToken cancellationToken = default)
	{
        try
        {
            var query = pageUri.Query;
            var queryParams = HttpUtility.ParseQueryString(query);

            string requestUrl = $"{pageUri.Scheme}://{pageUri.Host}/booksearch?page={webPage}&ask={queryParams["ask"]}";
            var document = await _htmlWeb.LoadFromWebAsync(requestUrl, cancellationToken);

            if (document.DocumentNode.SelectSingleNode("//*[@id=\"main\"]/p[2]") != null)
                return 0;

            var bookCount = document.DocumentNode.SelectNodes("//div[@id='main']/li")
                    .Count(x => x.OuterHtml.Contains("/b/"));

            return bookCount;
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
            _logger.LogError("Исключение при получении количества кол-ва страниц при поиске книг в BookCollectionParser.GetPageCountAsync(): {ex}", ex);
            return 0;
        }
    }

	private async Task<int> GetWebpageCountAsync(Uri booksUri, CancellationToken cancellationToken = default)
	{
        cancellationToken.ThrowIfCancellationRequested();

        try
		{
			var query = booksUri.Query;
			var queryParams = HttpUtility.ParseQueryString(query);

			var document = await _htmlWeb.LoadFromWebAsync(booksUri.AbsoluteUri, cancellationToken);
			var lastPageItem = document.DocumentNode.SelectSingleNode("//*[contains(concat(' ', normalize-space(@class), ' '), 'pager-last')]");

			if (lastPageItem != null)
			{
				var href = lastPageItem.FirstChild.Attributes["href"].Value;
				int pageNumberEndIndex = href.IndexOf('&');
				int pageNumberStartIndex = 17;
				var pageNumberString = href[pageNumberStartIndex..pageNumberEndIndex];
				int pageCount = int.Parse(pageNumberString);

				return pageCount;
			}

			return 0;
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
			_logger.LogError("Исключение при получении количества кол-ва страниц при поиске книг в BookCollectionParser.GetPageCountAsync(): {Ex}", ex);
            return 0;
		}
	}

	/// <returns>(book Id, author Id)</returns>
	private (int, int) GetIds(HtmlNode bookNode)
	{
		try
		{
			var items = bookNode.ChildNodes.Where(node => node.Name == "a");

			var bookIdString = string.Join("", items.First().Attributes["href"].Value[3..]);
			var authorIdString = string.Join("", items.Last().Attributes["href"].Value[3..]);

			var bookId = string.IsNullOrEmpty(authorIdString) ? 0 : int.Parse(bookIdString);
			var authorId = string.IsNullOrEmpty(authorIdString) ? 0 : int.Parse(authorIdString);

			return (bookId, authorId);
		}
		catch(Exception ex)
		{
			_logger.LogError("Исключение при получении Id автора или книги в BookCollectionParser.GetIds(): {ex}", ex);
			return (0, 0);
		}
	}
}

