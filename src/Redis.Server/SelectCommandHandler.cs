using RESP.DataTypes;

namespace Redis.Server;

public class SelectCommandHandler
{
    public static IRespData Handle(string[] args)
    {
        if (args.Length != 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("SELECT");
        }

        if (args[1] != "0")
        {
            return new RespSimpleError("ERR DB index is out of range");
        }

        return ReplyHelper.OK();
    }
}