using Flibusta.TelegramBot.Core.Abstractions;
using Flibusta.TelegramBot.Core.Entities;
using Flibusta.TelegramBot.Core.ResultPattern;
using Flibusta.TelegramBot.FlibustaApi.Parsers;
using Microsoft.Extensions.Logging;

namespace Flibusta.TelegramBot.FlibustaApi;

public class FlibustaApi : IFlibustaApi
{
	private const string FlibustaUrl = "https://flibusta.club";

	private readonly ILogger<FlibustaApi> _logger;
	private readonly IPageParser<Book> _bookParser;
	private readonly IPageParser<List<Book>> _bookCollectionParser;
	private readonly IBookCountProvider _bookCountProvider;

    public FlibustaApi(IPageParser<Book> bookParser, ILogger<FlibustaApi> logger, IPageParser<List<Book>> bookCollectionParser, IBookCountProvider bookCountProvider)
    {
        _bookParser = bookParser;
        _logger = logger;
        _bookCountProvider = bookCountProvider;
        _bookCollectionParser = bookCollectionParser;
    }

    public async Task<Result<List<Book>>> GetBooksByPageAsync(string bookTitle, int page, int pageSize, CancellationToken cancellationToken = default)
	{
		try
		{
			Uri bookPageUri = new($"{FlibustaUrl}/booksearch?ask={bookTitle}");

			var booksResult = await _bookCollectionParser.ParseAsync(bookPageUri, page, pageSize, cancellationToken);

			if (booksResult.IsFailure)
			{
				return new Error(booksResult.Error!.Message);
			}

			return booksResult;
		}
		catch (Exception ex)
		{
			_logger.LogError("Exception: {ex}", ex);

			return new Error($"Не удалось получить информацию о книгах c названием '{bookTitle}'");
		}
	}

	public async Task<Result<int>> GetBookCountAsync(string bookTitle, CancellationToken cancellationToken = default)
	{
        Uri bookPageUri = new($"{FlibustaUrl}/booksearch?ask={bookTitle}");

		return await _bookCountProvider.GetBookCountAsync(bookPageUri, cancellationToken);
    }

	public async Task<Result<Book>> GetBookAsync(int id, CancellationToken cancellationToken = default)
	{
		try
		{
			Uri bookPageUri = new($"{FlibustaUrl}/b/{id}");

			var bookResult = await _bookParser.ParseAsync(bookPageUri,cancellationToken: cancellationToken);

			if (bookResult.IsFailure)
			{
				return new Error(bookResult.Error!.Message);
			}

			return bookResult;
		}
		catch (Exception ex)
		{
			_logger.LogError("Exception: {ex}", ex);

			return new Error($"Не удалось получить информацию о книге с Id '{id}'");
		}
	}
}
