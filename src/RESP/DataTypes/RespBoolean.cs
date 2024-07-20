namespace RESP.DataTypes;

public record RespBoolean(bool Value) : IRespData
{
    public const string Prefix = "#";
}