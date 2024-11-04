using Flibusta.TelegramBot.Core.ResultPattern;

namespace Flibusta.TelegramBot.Core.Abstractions;

public interface IBookFileProvider
{
    Task<Result<Uri>> GetFileUri(Uri bookUri, CancellationToken cancellationToken = default);
}
