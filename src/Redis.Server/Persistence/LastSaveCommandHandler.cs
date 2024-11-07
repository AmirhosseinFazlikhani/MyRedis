using RESP.DataTypes;

namespace Redis.Server.Persistence;

public class LastSaveCommandHandler
{
    public static IRespData Handle(string[] args)
    {
        if (args.Length > 1)
        {
            return ReplyHelper.WrongArgumentsNumberError("LASTSAVE");
        }

        var value = RdbFile.LastSaveDateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
        return new RespInteger((long)value);
    }
}