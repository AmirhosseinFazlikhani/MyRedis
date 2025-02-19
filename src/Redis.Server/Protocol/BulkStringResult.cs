namespace Redis.Server.Protocol;

public class BulkStringResult(string? value) : IResult
{
    public string? Value { get; } = value;
}