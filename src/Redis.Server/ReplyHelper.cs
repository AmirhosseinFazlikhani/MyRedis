using RESP.DataTypes;

namespace Redis.Server;

public static class ReplyHelper
{
    public static RespSimpleString OK()
    {
        return new RespSimpleString("OK");
    }

    public static RespSimpleError IntegerParsingError()
    {
        return new RespSimpleError("ERR value is not an integer or out of range");
    }

    public static RespSimpleError WrongArgumentsNumberError(string command)
    {
        return new RespSimpleError($"ERR wrong number of arguments for '{command}' command");
    }

    public static RespSimpleError SyntaxError()
    {
        return new RespSimpleError("ERR syntax error");
    }
}