namespace Redis.Server.Protocol;

public readonly struct IntegerResult(long value) : IResult
{
    public long Value { get; } = value;
}