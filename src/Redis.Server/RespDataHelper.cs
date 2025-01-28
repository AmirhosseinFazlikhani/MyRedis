using Redis.Server.Protocol;

namespace Redis.Server;

public static class RespDataHelper
{
    public static string[] AsBulkStringArray(this IResult data)
    {
        if (data is not ArrayResult array)
        {
            throw new ProtocolException();
        }

        var items = array.Items.Select(i =>
        {
            if (i is not BulkStringResult bulkString)
            {
                throw new ProtocolException();
            }

            return bulkString;
        });
        return items.Select(i => i.Value ?? throw new ProtocolException()).ToArray();
    }
}