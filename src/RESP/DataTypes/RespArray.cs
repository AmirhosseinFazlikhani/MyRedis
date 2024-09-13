namespace RESP.DataTypes;

public record RespArray(IRespData[] Items) : IRespData
{
    public const char Prefix = '*';
}