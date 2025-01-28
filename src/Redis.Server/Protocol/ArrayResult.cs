namespace Redis.Server.Protocol;

public readonly struct ArrayResult(IResult[] items) : IResult
{
    public IResult[] Items { get; } = items;
}