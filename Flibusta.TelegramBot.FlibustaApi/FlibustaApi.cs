﻿using Flibusta.TelegramBot.Domain.Abstractions;
using Flibusta.TelegramBot.Domain.Entities;
using Flibusta.TelegramBot.Domain.ResultPattern;
using Microsoft.Extensions.Logging;

namespace Flibusta.TelegramBot.FlibustaApi;

public class FlibustaApi : IFlibustaApi
{
	private const string FlibustaUrl = "https://flibusta.club";

	private readonly ILogger<FlibustaApi> _logger;
	private readonly IPageParser<Book> _bookParser;
	private readonly IPageParser<List<Book>> _bookCollectionParser;

	public FlibustaApi(IPageParser<Book> bookParser, ILogger<FlibustaApi> logger, IPageParser<List<Book>> bookCollectionParser)
	{
		_bookParser = bookParser;
		_logger = logger;
		_bookCollectionParser = bookCollectionParser;
	}

	public async Task<Result<List<Book>>> GetBooksPageAsync(string bookTitle, int page, int pageSize, CancellationToken cancellationToken = default)
	{
		try
		{
			Uri bookPageUri = new($"{FlibustaUrl}/booksearch?ask={bookTitle}");

			var booksResult = await _bookCollectionParser.ParseAsync(bookPageUri, cancellationToken);

			if (booksResult.IsFailure)
			{
				return new Error(booksResult.Error!.Message);
			}

			var books = booksResult.Value!;

			return books.Skip((page - 1) * pageSize).Take(pageSize).ToList();
		}
		catch (Exception ex)
		{
			_logger.LogError("Exception: {ex}", ex);

			return new Error($"Не удалось получить информацию о книгах c названием '{bookTitle}'");
		}
	}

	public async Task<Result<Book>> GetBookAsync(int id, CancellationToken cancellationToken = default)
	{
		try
		{
			Uri bookPageUri = new($"{FlibustaUrl}/b/{id}");

			var bookResult = await _bookParser.ParseAsync(bookPageUri, cancellationToken);

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
