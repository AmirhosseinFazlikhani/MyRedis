using System.Net.Sockets;

namespace Redis.Server;

public static class ClientManager
{
    private static readonly List<ClientConnection> _clients = new();
    private static readonly object _clientsLock = new();

    public static async Task AcceptClientAsync(TcpListener tcpListener, CommandConsumer commandConsumer)
    {
        var lastClientId = 0;

        while (true)
        {
            var tcpClient = await tcpListener.AcceptTcpClientAsync();
            var client = new ClientConnection(++lastClientId, tcpClient, commandConsumer);

            lock (_clientsLock)
            {
                _clients.Add(client);
            }

            _ = client.StartAsync().ContinueWith(t =>
            {
                lock (_clientsLock)
                {
                    if (t.Status == TaskStatus.Faulted)
                    {
                        Console.Error.Write("Client {0}: {1}", client.ClientId, t.Exception);
                    }

                    _clients.Remove(client);
                }
            });
        }
    }
}