namespace Redis.Server.Protocol;

public class SimpleStringResult(string value) : IResult
{
    public string Value { get; } = value;
}