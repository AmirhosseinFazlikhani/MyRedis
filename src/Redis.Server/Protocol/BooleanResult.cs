namespace Redis.Server.Protocol;

public readonly struct BooleanResult(bool value) : IResult
{
    public bool Value { get; } = value;
}