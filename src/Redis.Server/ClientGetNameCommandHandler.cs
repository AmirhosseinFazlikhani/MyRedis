using RESP.DataTypes;

namespace Redis.Server;

public class ClientGetNameCommandHandler
{
    public static IRespData Handle(string[] args, ClientConnection client)
    {
        if (args.Length != 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("CLIENT GETNAME");
        }

        return new RespBulkString(client.ClientName);
    }
}