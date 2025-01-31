using Redis.Server.Replication;

namespace Redis.Server.CommandDispatching;

public class ReplicaOfCommandFactory : ICommandFactory
{
    private const string CommandName = "REPLICAOF";

    private readonly IClock _clock;
    private readonly CommandFactory _commandFactory;

    public ReplicaOfCommandFactory(IClock clock, CommandFactory commandFactory)
    {
        _clock = clock;
        _commandFactory = commandFactory;
    }

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length != 3)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        if (!int.TryParse(args[2], out var port))
        {
            return ReplyHelper.IntegerParsingError();
        }

        var nodeAddress = new NodeAddress(args[1], port);
        return new ReplicaOfCommand(_clock, nodeAddress, _commandFactory);
    }
}