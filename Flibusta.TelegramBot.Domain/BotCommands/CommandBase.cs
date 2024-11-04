using Telegram.Bot.Types;

namespace Flibusta.TelegramBot.Core.BotCommands;

public abstract class CommandBase
{
    public abstract string Name { get; }

    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    public abstract Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default);
}
