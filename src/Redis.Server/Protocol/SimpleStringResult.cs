namespace Redis.Server.Protocol;

public readonly struct SimpleStringResult(string value) : IResult
{
    public string Value { get; } = value;
}