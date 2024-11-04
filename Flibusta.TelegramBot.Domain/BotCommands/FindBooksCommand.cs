using Flibusta.TelegramBot.Core.Abstractions;
using Flibusta.TelegramBot.Core.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Flibusta.TelegramBot.Core.BotCommands;

public class FindBooksCommand : CommandBase
{
    private readonly ITelegramBotClient _bot;
    private readonly IFlibustaApi _flibustaApi;
    private readonly ILogger<FindBooksCommand> _logger;

    public FindBooksCommand(ITelegramBotClient bot, IFlibustaApi flibustaApi, ILogger<FindBooksCommand> logger)
    {
        _bot = bot;
        _logger = logger;
        _flibustaApi = flibustaApi;
    }

    public override string Name => CommandNames.FindBooks;

    /// <param name="args">(optional) [pageNumber]</param>
    public override async Task ExecuteAsync(Update update, string[]? args = null, CancellationToken cancellationToken = default)
    {
        string? userText;
        Message? message;

        if (update.Message?.Text != null)
        {
            userText = update.Message.Text;
            message = update.Message;
        }
        else
        {
            userText = update.CallbackQuery?.Message?.EntityValues?.First();
            message = update.CallbackQuery?.Message;
        }

        if (userText == null || message == null)
        {
            _logger.LogWarning("Некорректный вызов команды: {userText} или {message} является null", userText, message);
            return;
        }

        int bookCountPerPage = 8;
        int pageNumber = 1;
        var bookCountAllResult = await _flibustaApi.GetBookCountAsync(userText, cancellationToken);

        if (bookCountAllResult.IsFailure)
        {
            await _bot.SendTextMessageAsync(message.Chat, bookCountAllResult.Error!.Message, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }

        if (args == null ||
            args.Length == 0 ||
            !int.TryParse(args[0], out pageNumber))
        {
            var messageId = message.MessageId;

            await _bot.SendTextMessageAsync(message.Chat, $"Поиск книг '{userText}' 🔍", parseMode: ParseMode.Html, replyParameters: messageId, cancellationToken: cancellationToken);
        }

        var booksResult = await _flibustaApi.GetBooksByPageAsync(userText, pageNumber, bookCountPerPage, cancellationToken);

        if (booksResult.IsFailure)
        {
            await _bot.SendTextMessageAsync(message.Chat, booksResult.Error!.Message, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
            return;
        }

        var books = booksResult.Value!;
        var bookCount = books.Count;
        var responceMessage = new StringBuilder($"По запросу \"<b>{userText}</b>\" найдено книг - {bookCountAllResult.Value}:\n\n");
        var inlineMarkup = new InlineKeyboardMarkup();

        for (int i = 0; i < bookCount; i++)
        {
            var book = books[i];
            var bookNumber = i + 1;
            var bookEmoji = EmojiHelper.GetNumber(bookNumber);
            var btnCallbackData = $"{CommandNames.ShowBook} {book.Id}";

            responceMessage.Append($"{bookEmoji} <b>{book.Title}</b> - <i>{book.GetAuthors()}</i> \n\n");
            inlineMarkup.AddButton(bookNumber.ToString(), btnCallbackData);
        }

        responceMessage.Append("Выбери книгу, нажав на её номер снизу 👇");


        inlineMarkup.AddNewRow();
        var pageCount = (int)Math.Ceiling((decimal)bookCountAllResult.Value / bookCountPerPage);
        var buttonCurrentPage = InlineKeyboardButton.WithCallbackData($"{pageNumber}/{pageCount}", "_");
        var buttonLeft = InlineKeyboardButton.WithCallbackData("<", $"{CommandNames.FindBooks} {pageNumber - 1}");
        var buttonRight = InlineKeyboardButton.WithCallbackData(">", $"{CommandNames.FindBooks} {pageNumber + 1}");

        if (pageNumber == 1)
        {
            inlineMarkup.AddButton(buttonCurrentPage);
        }
        else
        {
            inlineMarkup.AddButton(buttonLeft)
                .AddButton(buttonCurrentPage);
        }
        if (pageNumber < pageCount)
        {
            inlineMarkup.AddButton(buttonRight);
        }

        if (args == null)
        {
            await _bot.SendTextMessageAsync(message.Chat, responceMessage.ToString(), replyMarkup: inlineMarkup, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
        else
        {
            await _bot.EditMessageTextAsync(message.Chat, message.MessageId, responceMessage.ToString(), replyMarkup: inlineMarkup, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }

    }
}
