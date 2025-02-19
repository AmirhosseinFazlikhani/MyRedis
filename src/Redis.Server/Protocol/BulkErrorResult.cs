namespace Redis.Server.Protocol;

public class BulkErrorResult(string value) : IResult, IError
{
    public string Value { get; } = value;
}