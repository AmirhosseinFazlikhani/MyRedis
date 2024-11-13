namespace RESP.DataTypes;

public record RespSimpleString(string Value) : IRespData
{
    public const char Prefix = '+';
}