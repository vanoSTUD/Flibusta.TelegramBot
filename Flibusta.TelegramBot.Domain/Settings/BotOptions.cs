namespace Flibusta.TelegramBot.Core.Settings;

public class BotOptions
{
    public const string Section = nameof(BotOptions);

    public string BotToken { get; set; } = default!;
    public Uri BotWebhookUrl { get; set; } = default!;
    public string SecretToken { get; set; } = default!;
  
}
