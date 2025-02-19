namespace Redis.Server.Protocol;

public class BigNumberResult(string value) : IResult
{
    public string Value { get; } = value;
}