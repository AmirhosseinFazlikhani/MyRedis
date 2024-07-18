using System.Net;
using System.Net.Sockets;

namespace Redis.Server;

class Program
{
    static async Task Main(string[] args)
    {
        var ip = "127.0.0.1";
        var port = 6379;

        var currentArgIndex = 0;
        while (currentArgIndex < args.Length)
        {
            switch (args[currentArgIndex])
            {
                case "-h":
                    ip = args[++currentArgIndex];
                    currentArgIndex++;
                    break;
                case "-p":
                    port = int.Parse(args[++currentArgIndex]);
                    currentArgIndex++;
                    break;
            }
        }

        TcpListener? server = null;

        try
        {
            server = new TcpListener(IPAddress.Parse(ip), port);
            server.Start();
            Console.WriteLine("Server is now listening on {0}:{1}", ip, port);

            var lastConnectionId = 0;

            while (true)
            {
                var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("New connection!");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                new Connection(++lastConnectionId, client).StartAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server?.Stop();
        }

        Console.WriteLine("\nHit enter to continue...");
        Console.Read();
    }
}