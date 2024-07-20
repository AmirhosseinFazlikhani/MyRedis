namespace RESP.DataTypes;

public record RespInteger(long Value) : IRespData
{
    public const string Prefix = ":";
}