using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Flibusta.TelegramBot.Core.BotCommands;

public class StartCommand : CommandBase
{
    private readonly ITelegramBotClient _bot;

    public StartCommand(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public override string Name => CommandNames.Start;

    public override async Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default)
    {
        ChatId? chatId = null;

        if (update.Message != null)
            chatId = update.Message.Chat.Id;
        else if (update.CallbackQuery != null)
            chatId = update.CallbackQuery.From.Id;

        if (chatId == null)
            return; // ToDo: добавить лог

        await _bot.SendTextMessageAsync(chatId, "Старт", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
    }
}
