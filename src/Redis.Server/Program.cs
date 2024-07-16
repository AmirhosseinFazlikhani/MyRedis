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
                Console.Write("Waiting for a connection... ");

                using var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Connected!");

                var networkStream = client.GetStream();

                var bytes = new byte[256];
                int readBytesCount;

                while ((readBytesCount = networkStream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var command = Encoding.UTF8.GetString(bytes, 0, readBytesCount);

                    if (command.Equals("ping", StringComparison.OrdinalIgnoreCase))
                    {
                        await networkStream.WriteAsync("PONG::"u8.ToArray());
                    }
                    else
                    {
                        networkStream.Write("Invalid command::"u8.ToArray());
                    }
                }
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