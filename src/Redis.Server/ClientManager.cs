using System.Net.Sockets;
using Serilog;

namespace Redis.Server;

public static class ClientManager
{
    private static readonly List<ClientConnection> _clients = new();
    private static readonly object _clientsLock = new();

    public static async Task AcceptClientAsync(TcpListener tcpListener, CommandHandler commandConsumer)
    {
        var lastClientId = 0;

        while (true)
        {
            var tcpClient = await tcpListener.AcceptTcpClientAsync();
            var client = new ClientConnection(++lastClientId, tcpClient);

            lock (_clientsLock)
            {
                _clients.Add(client);
            }

            Log.Information("Client {ClientId} connected", client.ClientId);

            _ = client.AcceptCommandsAsync(commandConsumer).ContinueWith(t =>
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
            });
        }
    }
}