using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Microsoft.Extensions.Logging;

namespace Flibusta.TelegramBot.Core.BotCommands;

internal class UndefinedCommand : CommandBase
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<UndefinedCommand> _logger;

    public UndefinedCommand(ITelegramBotClient bot, ILogger<UndefinedCommand> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public override string Name => CommandNames.Undefined;

    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="RequestException"></exception>
    public override async Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ChatId? chatId = null;

        if (update.Message != null)
            chatId = update.Message.Chat.Id;
        else if (update.CallbackQuery != null)
            chatId = update.CallbackQuery.From.Id;

        if (chatId == null)
            return; // ToDo: добавить лог

        try
        {
            await _bot.SendTextMessageAsync(chatId, "Не распознал команду! 🤔", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
        catch (RequestException ex)
        {
            _logger.LogCritical("Исключение от Telegram Api: {ex}", ex);
            await _bot.SendTextMessageAsync(chatId, "Не удалось найти книгу", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            return;
        }
    }
}
