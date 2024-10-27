using Flibusta.TelegramBot.Domain.Abstractions;
using Flibusta.TelegramBot.Domain.Entities;
using Flibusta.TelegramBot.Domain.ResultPattern;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Web;

namespace Flibusta.TelegramBot.FlibustaApi.Parsers;

public class BookCollectionParser : IPageParser<List<Book>>
{
	private readonly ILogger<BookCollectionParser> _logger;
	private readonly HtmlWeb _htmlWeb = new();

	public BookCollectionParser(ILogger<BookCollectionParser> logger)
	{
		_logger = logger;
	}

	public async Task<Result<List<Book>>> ParseAsync(Uri pageUri,  CancellationToken cancellationToken = default)
	{
		var queryParams = HttpUtility.ParseQueryString(pageUri.Query);
		var bookTitle = queryParams?["ask"];

		try
		{
			int pageCount = await GetPageCountAsync(pageUri, cancellationToken);
			var books = new List<Book>();

			for (int i = 0; i <= pageCount; i++)
			{
				var foundedBooks = await GetBooksByPageAsync(pageUri, i, cancellationToken);
				books.AddRange(foundedBooks);
			}

			if (books == null)
			{
				return new Error($"Не удалось получить информацию о книгах с названием '{bookTitle}'");
			}

			if (books.Count == 0)
			{
				return new Error($"Книг с названием '{bookTitle}' не найдено");
			}

			return books;
		}
		catch (Exception ex)
		{
			_logger.LogError("Exception: {e}", ex);

			return new Error($"Книг с названием '{bookTitle}' не найдено");
		}
	}

	private async Task<List<Book>> GetBooksByPageAsync(Uri booksUri, int page, CancellationToken cancellationToken = default)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();

			var query = booksUri.Query;
			var queryParams = HttpUtility.ParseQueryString(query);

			string requestUrl = $"{booksUri.Scheme}://{booksUri.Host}/booksearch?page={page}&ask={queryParams["ask"]}";
			var document = await _htmlWeb.LoadFromWebAsync(requestUrl, cancellationToken);

			var bookElements = document.DocumentNode.SelectNodes("//div[@id='main']/li")
					.Where(x => x.OuterHtml.Contains("/b/"));

			var books = new List<Book>();

			foreach (var element in bookElements)
			{
				var title = element.FirstChild.InnerText;
				var authorName = element.LastChild.InnerText;
				(var bookId, var authorId) = GetIds(element);

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
		catch 
		{
			_logger.LogError("Ecxeption при получении книг по номеру страницы в BookCollectionParser.GetBooksByPageAsync()");
			throw;
		}
	}

	private async Task<int> GetPageCountAsync(Uri booksUri, CancellationToken cancellationToken = default)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();

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
		catch
		{
			_logger.LogError("Ecxeption при получении количества кол-ва страниц при поиске книг в BookCollectionParser.GetPageCountAsync()");
			throw;
		}
	}

	/// <returns>(bookId, authorId)</returns>
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
		catch 
		{
			_logger.LogError("Ecxeption при получении Id автора или книги в BookCollectionParser.GetIds()");
			throw;
		}
	}
}
