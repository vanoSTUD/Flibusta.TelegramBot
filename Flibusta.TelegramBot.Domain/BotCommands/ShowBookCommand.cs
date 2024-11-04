using Flibusta.TelegramBot.Core.Abstractions;
using System.Text;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Flibusta.TelegramBot.Core.BotCommands;

public class ShowBookCommand : CommandBase
{
    private readonly ITelegramBotClient _bot;
    private readonly IFlibustaApi _flibustaApi;

    public ShowBookCommand(ITelegramBotClient bot, IFlibustaApi flibustaApi)
    {
        _bot = bot;
        _flibustaApi = flibustaApi;
    }

    public override string Name => CommandNames.ShowBook;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args">[bookId]</param>
    public override async Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default)
    {
        if (update.CallbackQuery is not { } callback)
            return;

        if (callback.Message is not { } message)
            return;

        if (args == null ||
            args.Length == 0 ||
            !int.TryParse(args[0], out int bookId))
        {
            return;
        }

        var bookResult = await _flibustaApi.GetBookAsync(bookId, cancellationToken);

        if (bookResult.IsFailure)
        {
            await _bot.SendTextMessageAsync(message.Chat, bookResult.Error!.Message, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
            return;
        }

        var book = bookResult.Value!;
        var bookTitle = HttpUtility.HtmlEncode(book.Title);
        var bookGenres = HttpUtility.HtmlEncode(book.GetGenres());
        var bookPublicationYear = HttpUtility.HtmlEncode(book.PublicationYear);
        var bookAdditionDate = HttpUtility.HtmlEncode(book.AdditionDate != null ? book.AdditionDate.Value.ToLongDateString() : "");
        var bookDescription = HttpUtility.HtmlEncode(book.Description);

        var responceMessage = $"""
            <b>📚{bookTitle}📚</b>

            Жанр: <i>{bookGenres}</i>
            Год публикации: <i>{bookPublicationYear}</i>
            Дата добавления: <i>{bookAdditionDate}</i>

            <i>{bookDescription}</i>
            """;

        if (book.PhotoUri != null)
        {
            var responseMessageLength = 1000;

            if (responceMessage.Length > responseMessageLength)
            {
                responceMessage = responceMessage[0..(responseMessageLength - 4)];
                responceMessage += "</i>";
            }

            await _bot.SendPhotoAsync(message.Chat, book.PhotoUri.AbsoluteUri, caption: responceMessage, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
            return;
        }

        await _bot.SendTextMessageAsync(message.Chat, responceMessage, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
    }
}