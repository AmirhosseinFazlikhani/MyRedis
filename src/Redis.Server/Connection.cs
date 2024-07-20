using System.Net.Sockets;
using System.Text;
using RESP;
using RESP.DataTypes;

namespace Redis.Server;

public class Connection : IDisposable
{
    private readonly int _id;
    private readonly TcpClient _tcpClient;

    public Connection(int id, TcpClient tcpClient)
    {
        _id = id;
        _tcpClient = tcpClient;
    }

    public async Task StartAsync()
    {
        var stream = _tcpClient.GetStream();

        var buffer = new byte[256];
        int readBytesCount;

        while ((readBytesCount = await stream.ReadAsync(buffer)) != 0)
        {
            if (buffer[0] != '*')
            {
                var error = Serializer.Serialize(new RespSimpleError("ERR Protocol error"));
                await stream.WriteAsync(Encoding.UTF8.GetBytes(error));
                continue;
            }

            var current = 1;
            while (buffer[current] != '\r')
            {
                current++;
            }

            var parametersCount = short.Parse(buffer.AsSpan(1..current));
            current += 2;
            var parameters = new string[parametersCount];

            while (parametersCount > 0)
            {
                parameters[^parametersCount] = ReadBulkString(stream, buffer, ref current, ref readBytesCount);
                parametersCount--;
            }

            var reply = CommandMediator.Send(parameters, new RequestContext { ConnectionId = _id });
            var serializedReply = (string)Serializer.Serialize((dynamic)reply);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(serializedReply));
        }
    }

    private static string ReadBulkString(NetworkStream stream, byte[] buffer, ref int offset, ref int end)
    {
        if (offset == end)
        {
            offset = 0;
            end = stream.Read(buffer);
        }

        var chunk = buffer.AsSpan(offset..end);

        var current = 1;
        while (chunk[current] != '\r')
        {
            current++;
        }

        var size = int.Parse(chunk[1..current]);
        current += 2;
        var totalSize = size + Serializer.TerminatorBytes.Length;

        if (totalSize <= chunk.Length - current)
        {
            offset += totalSize + current;
            return Encoding.UTF8.GetString(chunk[current..(size + current)]);
        }

        var result = new byte[size];
        chunk[current..].CopyTo(result);
        var remainingBytesCount = totalSize - chunk.Length - current;
        _ = stream.Read(result, chunk.Length - current, remainingBytesCount);
        offset = end = buffer.Length - 1;

        return Encoding.UTF8.GetString(result);
    }

    public void Dispose()
    {
        _tcpClient.Dispose();
    }
}