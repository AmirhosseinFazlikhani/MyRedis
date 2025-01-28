namespace Redis.Server.Protocol;

public readonly struct DoubleResult(double value) : IResult
{
    public double Value { get; } = value;
}