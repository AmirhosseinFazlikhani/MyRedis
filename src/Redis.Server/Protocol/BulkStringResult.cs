namespace Redis.Server.Protocol;

public readonly struct BulkStringResult(string? value) : IResult
{
    public string? Value { get; } = value;
}