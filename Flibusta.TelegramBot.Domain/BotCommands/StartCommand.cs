﻿using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Eventing.Reader;

namespace Flibusta.TelegramBot.Core.BotCommands;

public class StartCommand : CommandBase
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<StartCommand> _logger;

    public StartCommand(ITelegramBotClient bot, ILogger<StartCommand> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public override string Name => CommandNames.Start;

    /// <exception cref="OperationCanceledException"></exception>
    public override async Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ChatId? chatId = null;

        if (update.Message != null)
            chatId = update.Message.Chat.Id;
        else if (update.CallbackQuery != null)
            chatId = update.CallbackQuery.From.Id;

        if (chatId == null)
        {
            _logger.LogWarning("Некоректный вызов StartCommand.ExecuteAsync(). Update:  {Update}", update);
            return;
        }

        try
        {
            await _bot.SendTextMessageAsync(chatId, "Привет!\nМожешь писать название книги, что-нибудь подберём! 🤓", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
        catch (RequestException ex)
        {
            _logger.LogCritical("Исключение от Telegram Api: {ex}", ex);
            await _bot.SendTextMessageAsync(chatId, "Не удалось найти книгу", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
        catch(Exception)
        {
            return;
        }
    }
}
