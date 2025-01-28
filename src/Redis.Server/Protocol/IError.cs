namespace Redis.Server.Protocol;

public interface IError : IResult
{
    public string Value { get; }
}