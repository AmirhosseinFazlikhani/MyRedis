namespace RESP.DataTypes;

public record RespMap(KeyValuePair<RespSimpleString, IRespData>[] Entries) : IRespData
{
    public const string Prefix = "%";
}