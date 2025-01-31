using System.Net.Sockets;
using System.Text;
using Redis.Server.CommandDispatching;
using Redis.Server.Persistence;
using Redis.Server.Protocol;
using Serilog;

namespace Redis.Server.Replication;

public class Replica
{
    private readonly IClock _clock;
    private readonly CommandFactory _commandFactory;
    private readonly Task _task;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Replica(NodeAddress masterAddress, IClock clock, CommandFactory commandFactory)
    {
        _clock = clock;
        _commandFactory = commandFactory;
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
        await PlayPingPongAsync(stream, cancellationToken);
    }

    private async Task PlayPingPongAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        var command = new ArrayResult([new BulkStringResult("PING")]);
        await SendCommandAsync(command, networkStream, cancellationToken);

        int readBytes;
        var buffer = new byte[128];

        if ((readBytes = await networkStream.ReadAsync(buffer, cancellationToken)) == 0)
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        var reply = SerializerProvider.DefaultSerializer.Deserialize(buffer[..readBytes]);

        if (reply is not [SimpleStringResult { Value: "PONG" }])
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        await ConfigureAsync(networkStream, cancellationToken);
    }

    private async Task ConfigureAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        var commands = new ArrayResult[]
        {
            new([
                new BulkStringResult("REPLCONF"),
                new BulkStringResult("listening-port"),
                new BulkStringResult(Configuration.Port.ToString())
            ]),
            new([
                new BulkStringResult("REPLCONF"),
                new BulkStringResult("capa"),
                new BulkStringResult("psync2")
            ])
        };

        var serializedCommands = commands.Select(c => SerializerProvider.DefaultSerializer.Serialize(c))
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

        var reply = SerializerProvider.DefaultSerializer.Deserialize(buffer[..readBytes]);

        if (reply.Count != 2 || reply.Any(r => r is not SimpleStringResult { Value: "OK" }))
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        await InitialSyncAsync(networkStream, cancellationToken);
    }

    private async Task InitialSyncAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        var command = new ArrayResult([
            new BulkStringResult("PSYNC"),
            new BulkStringResult("?"),
            new BulkStringResult("-1"),
        ]);

        await SendCommandAsync(command, networkStream, cancellationToken);

        int readBytesCount;
        var buffer = new byte[1_000_000];

        if ((readBytesCount = await networkStream.ReadAsync(buffer, cancellationToken)) == 0)
        {
            Status = ReplicaStatus.Failed;
            return;
        }

        var reply = SerializerProvider.DefaultSerializer.Deserialize(buffer[..readBytesCount]);

        if (reply is not [SimpleStringResult stringReply])
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

        var buffer = new byte[256];
        int readBytesCount;
        var bufferSize = 0;
        using var semaphoreSlim = new SemaphoreSlim(0);

        while ((readBytesCount = await networkStream.ReadAsync(buffer.AsMemory(bufferSize..), cancellationToken)) != 0)
        {
            bufferSize += readBytesCount;
            var segmentIsCompletelyRead = buffer.AsSpan(..bufferSize) is [.., (byte)'\r', (byte)'\n'];

            if (!segmentIsCompletelyRead)
            {
                buffer = GrowBuffer(buffer);
                continue;
            }

            var commandArgs = SerializerProvider.DefaultSerializer.Deserialize(buffer[..bufferSize])
                .Select(RespDataHelper.AsBulkStringArray)
                .ToList();

            foreach (var args in commandArgs)
            {
                var command = _commandFactory.Create(args, new Scope());

                if (!command.IsSuccess)
                {
                    Log.Error("Failed to parse a command in replication stream: {Error}", command.Error!.Value);
                    return;
                }

                var reply = await HandleCommandAsync(command.Value!, cancellationToken);

                if (reply is IError error)
                {
                    Log.Error("Failed to handle a command in replication stream: {Error}", error.Value);
                    return;
                }
            }

            Offset += bufferSize;
            bufferSize = 0;

            await AckAsync(networkStream, cancellationToken);
        }
    }

    private async Task<IResult> HandleCommandAsync(ICommand command, CancellationToken cancellationToken)
    {
        var storedReply = default(IResult?);

        await CommandSynchronizer.PostAndWaitAsync(command, reply => storedReply = reply)
            .WaitAsync(cancellationToken);

        return storedReply!;
    }

    private async Task AckAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        var command = new ArrayResult([
            new BulkStringResult("REPLCONF"),
            new BulkStringResult("ACK"),
            new BulkStringResult(Offset.ToString()),
        ]);

        await SendCommandAsync(command, networkStream, cancellationToken);
    }

    private static async Task SendCommandAsync(ArrayResult command,
        NetworkStream networkStream,
        CancellationToken cancellationToken)
    {
        var serializedCommand = SerializerProvider.DefaultSerializer.Serialize(command);
        var commandBytes = Encoding.UTF8.GetBytes(serializedCommand);
        await networkStream.WriteAsync(commandBytes, cancellationToken);
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