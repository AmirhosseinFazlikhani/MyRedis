namespace Redis.Server.Protocol;

public readonly struct SimpleErrorResult(string value) : IError
{
    public string Value { get; } = value;
}