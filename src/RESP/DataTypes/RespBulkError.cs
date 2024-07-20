namespace RESP.DataTypes;

public record RespBulkError(string Value) : IRespData
{
    public const string Prefix = "!";
}