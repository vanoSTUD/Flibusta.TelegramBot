using Flibusta.TelegramBot.Core.BotCommands;
using Microsoft.OpenApi.Validations;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Flibusta.TelegramBot.API;

public class UpdateHandler
{
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ITelegramBotClient _bot;

    private readonly List<CommandBase> _commands;
    private readonly CommandBase _startCommand;
    private readonly CommandBase _findBooksCommand;

    public UpdateHandler(ILogger<UpdateHandler> logger, ITelegramBotClient bot, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _bot = bot;

        _commands = serviceProvider.GetServices<CommandBase>().ToList();
        _startCommand = _commands.First(c => c.Name == CommandNames.Start);
        _findBooksCommand = _commands.First(c => c.Name == CommandNames.FindBooks);
    }


    internal async Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException or TaskCanceledException)
        {
            _logger.LogWarning("Handle Task exception: {Ex}", exception);
            return;
        }

        _logger.LogError("HandleError: {Exception}", exception);

        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }


    internal async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await (update.Type switch
        {
            UpdateType.Message => HandleMessageAsync(update, cancellationToken),
            UpdateType.CallbackQuery => HandleCallbackAsync(update, cancellationToken),

            _ => _startCommand.ExecuteAsync(update, cancellationToken: cancellationToken)
        });
    }

    private async Task HandleMessageAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        if (message.Text is not { } userText)
            return;

       if (userText.StartsWith('/'))
       {
            var command = userText;
            await ExecuteCommandAsync(command, update, cancellationToken);
       }
       else
       {
            await _findBooksCommand.ExecuteAsync(update, cancellationToken: cancellationToken);
       }
    }

    private async Task HandleCallbackAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery is not { } callback)
            return;

        if (callback.Data is not { } data)
            return;

        if (data.StartsWith('/'))
        {
            await ExecuteCommandAsync(data, update, cancellationToken);
        }

        await _bot.AnswerCallbackQueryAsync(callback.Id, "📚", cancellationToken: cancellationToken);
    }

    private async Task ExecuteCommandAsync(string command, Update update, CancellationToken cancellationToken = default)
    {
        var commandName = command.Split(" ")[0];
        var args = command.Split(" ")[1..];
        var foundedCommand = _commands.FirstOrDefault(c => c.Name == commandName);

        if (foundedCommand == null)
        {
            return;
        }

        await foundedCommand.ExecuteAsync(update, args, cancellationToken: cancellationToken);
    }
}