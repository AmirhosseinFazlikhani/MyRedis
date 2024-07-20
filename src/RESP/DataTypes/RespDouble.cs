namespace RESP.DataTypes;

public record RespDouble(double Value) : IRespData
{
    public const string Prefix = ",";
}