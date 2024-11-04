using Flibusta.TelegramBot.Core.BotCommands;
using Microsoft.Extensions.DependencyInjection;

namespace Flibusta.TelegramBot.Core;

public static class DependencyInjection
{
    public static void AddBotCommands(this IServiceCollection services)
    {
        services.AddSingleton<CommandBase, StartCommand>();
        services.AddSingleton<CommandBase, FindBooksCommand>();
        services.AddSingleton<CommandBase, ShowBookCommand>();
        services.AddSingleton<CommandBase, UndefinedCommand>();
        services.AddSingleton<CommandBase, DownloadBookCommand>();
    }
}
