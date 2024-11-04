using Flibusta.TelegramBot.Core.ResultPattern;

namespace Flibusta.TelegramBot.Core.Abstractions;

public interface IBookCountProvider
{
    Task<Result<int>> GetBookCountAsync(Uri pageUri, CancellationToken cancellationToken = default);
}
