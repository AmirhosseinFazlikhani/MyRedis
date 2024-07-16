using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Redis.Server;

class Program
{
    static async Task Main(string[] args)
    {
        var port = args.Length > 1 && args[0] == "-p" ? int.Parse(args[1]) : 5000;
        
        TcpListener? server = null;

        try
        {
            var localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();

            while (true)
            {
                CommandListener.ListenAsync(await server.AcceptTcpClientAsync()).ConfigureAwait(false);
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