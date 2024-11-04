using Flibusta.TelegramBot.API;
using Flibusta.TelegramBot.Core;
using Flibusta.TelegramBot.Core.Settings;
using Flibusta.TelegramBot.FlibustaApi;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var botOptionSection = configuration.GetSection(BotOptions.Section);

builder.Services.Configure<BotOptions>(botOptionSection);

var botOptions = botOptionSection.Get<BotOptions>()!;
var botToken = botOptions.BotToken;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient("webhook").RemoveAllLoggers().AddTypedClient<ITelegramBotClient>(
    httpClient => new TelegramBotClient(botToken, httpClient));

builder.Services.AddFlibustaApi();
builder.Services.AddBotCommands();

builder.Services.ConfigureTelegramBotMvc();

builder.Services.AddSingleton<UpdateHandler>();

var app = builder.Build();

app.MapControllers();

var bot = app.Services.GetRequiredService<ITelegramBotClient>();
var webhookUrl = botOptions.BotWebhookUrl.AbsoluteUri;
var secretToken = botOptions.SecretToken;

await bot.SetWebhookAsync(webhookUrl, allowedUpdates: [], secretToken: secretToken);
Console.WriteLine($"Webhook set to {webhookUrl}");

app.Run();
