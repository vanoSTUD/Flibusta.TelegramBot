using Flibusta.TelegramBot.Core.ResultPattern;

namespace Flibusta.TelegramBot.Core.Abstractions;

public interface IPageParser<T>
{
    public Task<Result<T>> ParseAsync(Uri pageUri, int page = default, int pageSize = default, CancellationToken cancellationToken = default);
}
