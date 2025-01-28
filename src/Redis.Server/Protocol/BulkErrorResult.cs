namespace Redis.Server.Protocol;

public readonly struct BulkErrorResult(string value) : IResult, IError
{
    public string Value { get; } = value;
}