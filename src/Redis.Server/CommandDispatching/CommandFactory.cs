using Redis.Server.Protocol;

namespace Redis.Server.CommandDispatching;

public class CommandFactory
{
    private readonly ICommandFactory[] _factories;

    public CommandFactory(IClock clock)
    {
        _factories =
        [
            new SetCommandFactory(clock),
            new GetCommandFactory(clock),
            new SelectCommandFactory(),
            new SaveCommandFactory(clock),
            new ReplicaOfCommandFactory(clock, this),
            new PingCommandFactory(),
            new LastSaveCommandFactory(),
            new KeysCommandFactory(clock),
            new HelloCommandFactory(),
            new ExpireCommandFactory(clock),
            new DelCommandFactory(),
            new ConfigGetCommandFactory(),
            new ClientGetNameCommandFactory(),
            new ClientSetNameCommandFactory(),
            new BGSaveCommandFactory(clock),
        ];
    }

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        var factory = _factories.FirstOrDefault(f => f.Matches(args));

        if (factory is null)
        {
            return new SimpleErrorResult($"ERR unknown command '{args[0]}'");
        }

        return factory.Create(args, scope);
    }
}