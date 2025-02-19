namespace Redis.Server.Protocol;

public class ArrayResult(IResult[] items) : IResult
{
    public IResult[] Items { get; } = items;
}