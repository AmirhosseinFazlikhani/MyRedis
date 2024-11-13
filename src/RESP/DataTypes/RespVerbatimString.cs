namespace RESP.DataTypes;

public record RespVerbatimString(string Value, string Encoding) : IRespData
{
    public const char Prefix = '=';
}