namespace Redis.Server.Protocol;

public class MapResult(KeyValuePair<SimpleStringResult, IResult>[] entries) : IResult
{
    public KeyValuePair<SimpleStringResult, IResult>[] Entries { get; } = entries;
}