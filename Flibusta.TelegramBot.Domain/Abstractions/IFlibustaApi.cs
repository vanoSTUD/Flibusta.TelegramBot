using Flibusta.TelegramBot.Domain.Entities;
using Flibusta.TelegramBot.Domain.ResultPattern;

namespace Flibusta.TelegramBot.Domain.Abstractions;

public interface IFlibustaApi
{
	public Task<Result<Book>> GetBookAsync(int id, CancellationToken cancellationToken = default);
	public Task<Result<List<Book>>> GetBooksPageAsync(string bookTitle, int page, int pageSize, CancellationToken cancellationToken = default);
}
