using System.Net.Sockets;
using Redis.Server.CommandDispatching;
using Serilog;

namespace Redis.Server;

public static class ClientManager
{
    private static readonly List<ClientConnection> _clients = new();
    private static readonly object _clientsLock = new();

    public static async Task AcceptClientAsync(TcpListener tcpListener,
        CommandFactory commandFactory,
        CancellationToken cancellationToken)
    {
        var lastClientId = 0;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var tcpClient = await tcpListener.AcceptTcpClientAsync(cancellationToken);
            var client = new ClientConnection(++lastClientId, tcpClient, commandFactory);

            lock (_clientsLock)
            {
                _clients.Add(client);
            }

            Log.Information("Client {ClientId} connected", client.ClientId);

            _ = client.AcceptCommandsAsync(cancellationToken)
                .ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.Faulted)
                        {
                            Log.Error(t.Exception, "Client {ClientId} disconnected", client.ClientId);
                        }
                        else
                        {
                            Log.Information("Client {ClientId} closed the connection", client.ClientId);
                        }

                        lock (_clientsLock)
                        {
                            _clients.Remove(client);
                        }
                    },
                    cancellationToken);
        }
    }
}