namespace Flibusta.TelegramBot.Domain.ResultPattern;

public class Result<TValue>
{
    private Result(TValue value)
    {
        Value = value;
        Error = null;
    }

    private Result(Error error)
    {
        Error = error;
        Value = default;
    }

    public TValue? Value { get; private set; }
    public Error? Error { get; private set; }
    public bool IsSuccess => Error != null;
    public bool IsFailure => Error == null;

    public static Result<TValue> Failure(Error error) => new Result<TValue>(error);

    public static Result<TValue> Success(TValue value) => new Result<TValue>(value);


    public static implicit operator Result<TValue>(TValue value) => new Result<TValue>(value);

    public static implicit operator Result<TValue>(Error error) => new Result<TValue>(error);


    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}
