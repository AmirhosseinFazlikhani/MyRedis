namespace Redis.Server.CommandDispatching;

public class HelloCommandFactory : ICommandFactory
{
    private const string CommandName = "HELLO";

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length > 2)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        if (args.Length == 1)
        {
            return new HelloCommand(scope.Client);
        }

        if (!int.TryParse(args[1], out var protocolVersion))
        {
            return ReplyHelper.IntegerParsingError();
        }

        return new HelloCommand(scope.Client, protocolVersion);
    }
}