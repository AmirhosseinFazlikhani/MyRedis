namespace Redis.Server.Protocol;

public interface IResultSerializer
{
    string Serialize(IResult value);
    List<IResult> Deserialize(byte[] value);
}