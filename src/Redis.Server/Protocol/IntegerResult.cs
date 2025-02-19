namespace Redis.Server.Protocol;

public class IntegerResult(long value) : IResult
{
    public long Value { get; } = value;
}