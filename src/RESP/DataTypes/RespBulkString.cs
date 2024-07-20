namespace RESP.DataTypes;

public record RespBulkString(string? Value) : IRespData
{
    public const string Prefix = "$";
}