namespace Redis.Server.Protocol;

public class VerbatimStringResult(string value, string encoding) : IResult
{
    public string Value { get; } = value;
    public string Encoding { get; } = encoding;
}