using RESP.DataTypes;

namespace Redis.Server;

public static class RespDataHelper
{
    public static string[] AsBulkStringArray(this IRespData data)
    {
        var array = data as RespArray ?? throw new ProtocolException();
        var items = array.Items.Select(i => i as RespBulkString ?? throw new ProtocolException());
        return items.Select(i => i.Value ?? throw new ProtocolException()).ToArray();
    }
}