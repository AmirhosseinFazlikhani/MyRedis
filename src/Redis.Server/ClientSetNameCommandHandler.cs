using RESP.DataTypes;

namespace Redis.Server;

public class ClientSetNameCommandHandler
{
    public static IRespData Handle(string[] args, ClientConnection client)
    {
        if (args.Length != 3)
        {
            return ReplyHelper.WrongArgumentsNumberError("CLIENT SETNAME");
        }

        client.ClientName = args[2];
        return ReplyHelper.OK();
    }
}