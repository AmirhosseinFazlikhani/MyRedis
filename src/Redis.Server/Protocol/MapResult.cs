namespace Redis.Server.Protocol;

public readonly struct MapResult(KeyValuePair<SimpleStringResult, IResult>[] entries) : IResult
{
    public KeyValuePair<SimpleStringResult, IResult>[] Entries { get; } = entries;
}