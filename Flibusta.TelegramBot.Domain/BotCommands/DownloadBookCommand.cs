using Microsoft.Extensions.Logging;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Flibusta.TelegramBot.Core.Abstractions;
using Flibusta.TelegramBot.Core.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flibusta.TelegramBot.Core.BotCommands;

internal class DownloadBookCommand : CommandBase
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<StartCommand> _logger;
    private readonly IFlibustaApi _flibustaApi;

    public DownloadBookCommand(ITelegramBotClient bot, ILogger<StartCommand> logger, IFlibustaApi flibustaApi)
    {
        _bot = bot;
        _logger = logger;
        _flibustaApi = flibustaApi;
    }

    public override string Name => CommandNames.Download;

    /// <exception cref="OperationCanceledException"></exception>
    /// <param name="args">CallbackQuery data: [bookId, downloadTypeName(TXT, FB2...)]</param>
    public override async Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (update.CallbackQuery is not { } callback)
            return;
        if (callback.Message is not { } message)
            return;

        if (args == null ||
            args.Length < 2 ||
            !int.TryParse(args[0], out var bookId) ||
            string.IsNullOrEmpty(args[1]))
        {
            _logger.LogWarning("Некорректный вызов DownloadBookCommand.ExecuteAsync(). [args] не прошел валидацию. Не удалось получить bookId или downloadType");
            return;
        }

        try
        {
            var bookResult = await _flibustaApi.GetBookAsync(bookId, cancellationToken);

            if (bookResult.IsFailure)
            {
                await _bot.SendTextMessageAsync(message.Chat, bookResult.Error!.Message, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                return;
            }

            var book = bookResult.Value!;
            var inlineMarkup = new InlineKeyboardMarkup();
            var bookDownloadLinks = book.DownloadLinks;
            var downloadTypeName = args[1];

            foreach (var link in bookDownloadLinks)
            {
                if (link.Name == downloadTypeName)
                    continue;

                var buttonArgsData = $"{CommandNames.Download} {bookId} {link.Name}";
                inlineMarkup.AddButton(link.Name, buttonArgsData);
            }

            var currentDownloadLink = bookDownloadLinks.First(l => l.Name == downloadTypeName).Uri!;
            var documentUri = await _flibustaApi.GetBookFileUri(currentDownloadLink, cancellationToken);

            if (documentUri.IsFailure)
            {
                await _bot.SendTextMessageAsync(message.Chat, documentUri.Error!.Message, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                return;
            }

            var responceMessage = $"""
                                    <b>Книга:</b> <i>{book.Title}</i>
                                    <b>Автор:</b> <i>{book.GetAuthors()}</i>
                                    <b>Файл:</b> <i>{downloadTypeName}</i>
                                    """;

            await _bot.DeleteMessageAsync(message.Chat, message.MessageId, cancellationToken: cancellationToken);
            await _bot.SendDocumentAsync(message.Chat, documentUri.Value!.AbsoluteUri, caption: responceMessage, replyMarkup: inlineMarkup, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
        catch (RequestException ex)
        {
            _logger.LogCritical("Исключение от Telegram Api: {ex}", ex);
            await _bot.SendTextMessageAsync(message.Chat, "Не удалось найти файл", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключение в DownloadBookCommand.ExecuteAsync(): {ex}", ex);

            await _bot.SendTextMessageAsync(message.Chat, "Не удалось найти файл", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
    }
}
