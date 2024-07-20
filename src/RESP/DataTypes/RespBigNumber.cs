namespace RESP.DataTypes;

public record RespBigNumber(string Value) : IRespData
{
    public const string Prefix = "(";
}