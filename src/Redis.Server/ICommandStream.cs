using RESP.DataTypes;

namespace Redis.Server;

public interface ICommandStream
{
    IAsyncEnumerable<string[]> ListenAsync();
    
    Task ReplyAsync(IRespData value);
}