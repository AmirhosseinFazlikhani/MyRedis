using Redis.Server.Protocol;

namespace Redis.Server;

public record ErrorOr<T>
{
    public T? Value { get; private init; }
    public IError? Error { get; private init; }
    public bool IsSuccess { get; private init; }
    
    public static implicit operator ErrorOr<T>(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static implicit operator ErrorOr<T>(SimpleErrorResult error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    public static implicit operator ErrorOr<T>(BulkErrorResult error) => new()
    {
        IsSuccess = false,
        Error = error
    };
}