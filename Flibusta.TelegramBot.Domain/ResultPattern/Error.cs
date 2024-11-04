namespace Flibusta.TelegramBot.Core.ResultPattern;

public class Error
{
    public Error(string message, string? description = default)
    {
        Message = message;
        Description = description;
    }

    public string Message { get; private set; }
    public string? Description { get; private set; }
}