namespace Redis.Server.Protocol;

public class DoubleResult(double value) : IResult
{
    public double Value { get; } = value;
}