using System.Net.Sockets;

namespace Redis.Server;

public static class SessionFactory
{
    private static int _lastId;

    public static Session Create(IClock clock, TcpClient tcpClient)
    {
        var stream = tcpClient.GetStream();

        var session = new Session(++_lastId,
            clock,
            new CommandMediator(clock),
            new CommandStream(stream));

        return session;
    }
}