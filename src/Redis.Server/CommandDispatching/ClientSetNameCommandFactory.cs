namespace Redis.Server.CommandDispatching;

public class ClientSetNameCommandFactory : ICommandFactory
{
    public bool Matches(string[] args) => args.StartWith(["CLIENT", "SETNAME"]);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length != 3)
        {
            return ReplyHelper.WrongArgumentsNumberError("CLIENT SETNAME");
        }

        return new ClientSetNameCommand(scope.Client, args[2]);
    }
}