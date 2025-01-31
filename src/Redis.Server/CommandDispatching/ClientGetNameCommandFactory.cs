namespace Redis.Server.CommandDispatching;

public class ClientGetNameCommandFactory : ICommandFactory
{
    public bool Matches(string[] args) => args.StartWith(["CLIENT", "GETNAME"]);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        return new ClientGetNameCommand(scope.Client);
    }
}