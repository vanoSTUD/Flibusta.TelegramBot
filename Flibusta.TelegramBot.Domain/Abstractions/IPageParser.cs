using Flibusta.TelegramBot.Domain.ResultPattern;

namespace Flibusta.TelegramBot.Domain.Abstractions;

public interface IPageParser<T>
{
    public Task<Result<T>> ParseAsync(Uri pageUri, CancellationToken cancellationToken = default);
}
