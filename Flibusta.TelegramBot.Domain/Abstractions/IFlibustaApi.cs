using Flibusta.TelegramBot.Core.Entities;
using Flibusta.TelegramBot.Core.ResultPattern;

namespace Flibusta.TelegramBot.Core.Abstractions;

public interface IFlibustaApi
{
	Task<Result<Book>> GetBookAsync(int id, CancellationToken cancellationToken = default);
	Task<Result<List<Book>>> GetBooksByPageAsync(string bookTitle, int page, int pageSize, CancellationToken cancellationToken = default);
	Task<Result<int>> GetBookCountAsync(string bookTitle, CancellationToken cancellationToken = default);
	Task<Result<Uri>> GetBookFileUri(Uri bookUri, CancellationToken cancellation = default);
}
