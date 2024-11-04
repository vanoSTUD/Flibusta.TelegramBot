using Telegram.Bot.Types;

namespace Flibusta.TelegramBot.Core.BotCommands;

public abstract class CommandBase
{
    public abstract string Name { get; }
    public abstract Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default);
}
