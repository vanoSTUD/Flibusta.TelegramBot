namespace Flibusta.TelegramBot.Domain.ResultPattern;

public class Error
{
    public Error(Exception exception)
    {
        Exception = exception;
    }

    public Error(string message, string? description)
    {
        Message = message;
        Description = description;
    }

    public string? Message { get; private set; }
    public string? Description { get; private set; }
    public Exception? Exception { get; private set; }
}