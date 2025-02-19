namespace Redis.Server.Protocol;

public class SimpleErrorResult(string value) : IError
{
    public string Value { get; } = value;
}