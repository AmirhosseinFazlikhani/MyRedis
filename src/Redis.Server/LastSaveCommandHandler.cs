using RESP.DataTypes;

namespace Redis.Server;

public class LastSaveCommandHandler
{
    public static IRespData Handle(string[] args)
    {
        if (args.Length > 1)
        {
            return ReplyHelper.WrongArgumentsNumberError("LASTSAVE");
        }

        var value = Persistence.LastSaveDateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
        return new RespInteger((long)value);
    }
}