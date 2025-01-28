namespace Redis.Server.Protocol;

public readonly struct BigNumberResult(string value) : IResult
{
    public string Value { get; } = value;
}