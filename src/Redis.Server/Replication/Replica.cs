using System.Net.Sockets;
using System.Text;
using Redis.Server.Persistence;
using RESP;
using RESP.DataTypes;
using Serilog;

namespace Redis.Server.Replication;

public class Replica
{
    private readonly IClock _clock;
    private readonly ICommandHandler _commandHandler;
    private readonly Task _task;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Replica(NodeAddress masterAddress, IClock clock, ICommandHandler commandHandler)
    {
        _clock = clock;
        _commandHandler = commandHandler;
        Status = ReplicaStatus.Initializing;

        _task = ConnectAsync(masterAddress, _cancellationTokenSource.Token)
            .ContinueWith(t =>
            {
                Status = ReplicaStatus.Failed;

                if (t.IsFaulted)
                {
                    Log.Error(t.Exception, "Replica faulted");
                }

                Log.Information("Master connection closed!");
            });

        Task.Run(() => _task);
    }

    public string? ReplicationId { get; private set; }
    public long Offset { get; private set; }
    public ReplicaStatus Status { get; private set; }

    public async Task CancelAsync()
    {
        if (Status == ReplicaStatus.Failed)
        {
            return;
        }

        await _cancellationTokenSource.CancelAsync();
        await _task;
    }

    private async Task ConnectAsync(NodeAddress masterAddress, CancellationToken cancellationToken)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(masterAddress.Host, masterAddress.Port, cancellationToken);
        var stream = tcpClient.GetStream();
        await PingAsync(stream, cancellationToken);
    }

    private async Task PingAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        var command = new RespArray([new RespBulkString("PING")]);
        var serializedCommand = Resp2Serializer.Serialize(command);
        var commandBytes = Encoding.UTF8.GetBytes(serializedCommand);
        await networkStream.WriteAsync(commandBytes, cancellationToken);

        int readBytes;
        var buffer = new byte[128];

        if ((readBytes = await networkStream.ReadAsync(buffer, cancellationToken)) == 0)
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        var reply = Resp2Serializer.Deserialize(buffer[..readBytes]);

        if (reply is not [RespSimpleString { Value: "PONG" }])
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        await ConfigureAsync(networkStream, cancellationToken);
    }

    private async Task ConfigureAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        var commands = new RespArray[]
        {
            new([
                new RespBulkString("REPLCONF"),
                new RespBulkString("listening-port"),
                new RespBulkString(Configuration.Port.ToString())
            ]),
            new([
                new RespBulkString("REPLCONF"),
                new RespBulkString("capa"),
                new RespBulkString("psync2")
            ])
        };

        var serializedCommands = commands.Select(Resp2Serializer.Serialize)
            .Select(Encoding.UTF8.GetBytes)
            .Aggregate((p, c) => p.Concat(c).ToArray())
            .ToArray();

        await networkStream.WriteAsync(serializedCommands, cancellationToken);

        int readBytes;
        var buffer = new byte[128];

        if ((readBytes = await networkStream.ReadAsync(buffer, cancellationToken)) == 0)
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        var reply = Resp2Serializer.Deserialize(buffer[..readBytes]);

        if (reply.Count != 2 || reply.Any(r => r is not RespSimpleString { Value: "OK" }))
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        await InitialSyncAsync(networkStream, cancellationToken);
    }

    private async Task InitialSyncAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        var command = new RespArray([
            new RespBulkString("PSYNC"),
            new RespBulkString("?"),
            new RespBulkString("-1"),
        ]);

        var serializedCommand = Resp2Serializer.Serialize(command);
        var commandBytes = Encoding.UTF8.GetBytes(serializedCommand);
        await networkStream.WriteAsync(commandBytes, cancellationToken);

        int readBytesCount;
        var buffer = new byte[1_000_000];

        if ((readBytesCount = await networkStream.ReadAsync(buffer, cancellationToken)) == 0)
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        var reply = Resp2Serializer.Deserialize(buffer[..readBytesCount]);

        if (reply is not [RespSimpleString stringReply])
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        var replyParts = stringReply.Value.Split(' ');

        if (replyParts is not ["FULLRESYNC", var replicationId, var offset]
            || !long.TryParse(offset, out var longOffset))
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        ReplicationId = replicationId;
        Offset = longOffset;

        var tempRdbFileName = Path.Combine(Configuration.Directory, Guid.NewGuid().ToString());
        var tempRdbFile = File.OpenWrite(tempRdbFileName);

        try
        {
            while (networkStream.ReadByte() != '$')
            {
            }

            int readByte;
            var fileLengthAsString = string.Empty;
            while ((readByte = networkStream.ReadByte()) != '\r')
            {
                fileLengthAsString += (char)readByte;
            }

            var fileLength = long.Parse(fileLengthAsString);

            // Discard \n
            networkStream.ReadByte();

            while (fileLength > 0)
            {
                if (fileLength <= buffer.Length)
                {
                    readBytesCount = (int)fileLength;
                    await networkStream.ReadExactlyAsync(buffer.AsMemory(..readBytesCount), cancellationToken);
                }
                else
                {
                    readBytesCount = await networkStream.ReadAsync(buffer, cancellationToken);
                }

                await tempRdbFile.WriteAsync(buffer.AsMemory()[..readBytesCount], cancellationToken);
                await tempRdbFile.FlushAsync(cancellationToken);
                fileLength -= readBytesCount;
            }

            tempRdbFile.Close();
            RdbFile.Load(_clock, tempRdbFileName);
        }
        finally
        {
            tempRdbFile.Close();
            File.Delete(tempRdbFileName);
        }

        await ListenToIncomingCommandsAsync(networkStream, cancellationToken);
    }

    private async Task ListenToIncomingCommandsAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        Status = ReplicaStatus.Running;

        var buffer = new byte[1000];
        int readBytesCount;
        var offset = 0;

        while ((readBytesCount = await networkStream.ReadAsync(buffer.AsMemory(offset..), cancellationToken)) != 0)
        {
            offset += readBytesCount;

            if (buffer[..offset] is not [.., (byte)'\r', (byte)'\n'])
            {
                buffer = GrowBuffer(buffer);
                offset += readBytesCount;
                continue;
            }

            var commands = Resp2Serializer.Deserialize(buffer[..offset])
                .Select(RespDataHelper.AsBulkStringArray)
                .ToList();

            foreach (var command in commands)
            {
                _commandHandler.Handle(command);
            }

            Offset += offset;
            offset = 0;
        }
    }

    private static byte[] GrowBuffer(byte[] buffer)
    {
        var newBufferSize = buffer.Length * 2;

        if (newBufferSize < 0)
        {
            newBufferSize = int.MaxValue;
        }

        var newBuffer = new byte[newBufferSize];
        Array.Copy(buffer, newBuffer, buffer.Length);
        return newBuffer;
    }
}