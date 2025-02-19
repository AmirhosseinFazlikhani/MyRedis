namespace Redis.Server.Protocol;

public class BooleanResult(bool value) : IResult
{
    public bool Value { get; } = value;
}