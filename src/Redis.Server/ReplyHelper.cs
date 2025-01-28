using Redis.Server.Protocol;

namespace Redis.Server;

public static class ReplyHelper
{
    public static SimpleStringResult OK()
    {
        return new SimpleStringResult("OK");
    }

    public static SimpleErrorResult IntegerParsingError()
    {
        return new SimpleErrorResult("ERR value is not an integer or out of range");
    }

    public static SimpleErrorResult WrongArgumentsNumberError(string command)
    {
        return new SimpleErrorResult($"ERR wrong number of arguments for '{command}' command");
    }

    public static SimpleErrorResult SyntaxError()
    {
        return new SimpleErrorResult("ERR syntax error");
    }
}