using System.Net;
using System.Net.Sockets;
using System.Text;

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

            while (true)
            {
                CommandListener.ListenAsync(await server.AcceptTcpClientAsync());
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