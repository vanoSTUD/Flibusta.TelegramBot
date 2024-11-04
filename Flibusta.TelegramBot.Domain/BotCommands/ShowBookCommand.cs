using Flibusta.TelegramBot.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flibusta.TelegramBot.Core.BotCommands;

public class ShowBookCommand : CommandBase
{
    private readonly ITelegramBotClient _bot;
    private readonly IFlibustaApi _flibustaApi;
    private readonly ILogger<ShowBookCommand> _logger;

    public ShowBookCommand(ITelegramBotClient bot, IFlibustaApi flibustaApi, ILogger<ShowBookCommand> logger)
    {
        _bot = bot;
        _logger = logger;
        _flibustaApi = flibustaApi;
    }

    public override string Name => CommandNames.ShowBook;

    /// <param name="args">CallbackQuery data: [bookId]</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    public override async Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (update.CallbackQuery is not { } callback)
            return;

        if (callback.Message is not { } message)
            return;

        if (args == null ||
            args.Length == 0 ||
            !int.TryParse(args[0], out int bookId))
        {
            await _bot.SendTextMessageAsync(message.Chat, "Не удалось найти книгу", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
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
            var bookTitle = HttpUtility.HtmlEncode(book.Title) ?? "";
            var BookAuthors = HttpUtility.HtmlEncode(book.GetAuthors()) ?? "";
            var bookGenres = HttpUtility.HtmlEncode(book.GetGenres()) ?? ""; 
            var bookPublicationYear = HttpUtility.HtmlEncode(book.PublicationYear) ?? "";
            var bookAdditionDate = HttpUtility.HtmlEncode(book.AdditionDate != null ? book.AdditionDate.Value.ToLongDateString() : "") ?? "";
            var bookDescription = HttpUtility.HtmlEncode(book.Description) ?? "";

            var responceMessage = $"""
            <b>📚{bookTitle}📚</b>

            <b>Автор:</b> <i>{BookAuthors}</i>
            <b>Жанр:</b> <i>{bookGenres}</i>
            <b>Год публикации:</b> <i>{bookPublicationYear}</i>
            <b>Дата добавления:</b> <i>{bookAdditionDate}</i>

            <i>{bookDescription}</i>
            """;

            var inlineMarkup = new InlineKeyboardMarkup();
            var bookDownloadLinks = book.DownloadLinks;

            foreach (var link in bookDownloadLinks)
            {
                if (string.IsNullOrEmpty(link.Name) || link.Uri == null)
                    continue;

                var buttonArgsData = $"{CommandNames.Download} {bookId} {link.Name}";
                inlineMarkup.AddButton(link.Name, buttonArgsData);
            }

            if (book.PhotoUri != null)
            {
                var responseMessageLength = 1000;

                if (responceMessage.Length > responseMessageLength)
                {
                    responceMessage = responceMessage[0..(responseMessageLength - "...</i>".Length)];
                    responceMessage += "...</i>";
                }

                await _bot.SendPhotoAsync(message.Chat, book.PhotoUri.AbsoluteUri, caption: responceMessage, replyMarkup: inlineMarkup, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                return;
            }

            await _bot.SendTextMessageAsync(message.Chat, responceMessage, parseMode: ParseMode.Html, replyMarkup: inlineMarkup, cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(RequestException ex)
        {
            _logger.LogCritical("Исключение от Telegram Api: {ex}", ex);
            await _bot.SendTextMessageAsync(message.Chat, "Не удалось найти книгу", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError("Исключение при работе ShowBookCommand.ExecuteAsync(): {ex}", ex);
            await _bot.SendTextMessageAsync(message.Chat, "Не удалось найти книгу", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
    }
}