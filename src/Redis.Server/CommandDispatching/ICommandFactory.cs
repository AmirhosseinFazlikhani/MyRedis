namespace Redis.Server.CommandDispatching;

public interface ICommandFactory
{
    bool Matches(string[] args);
    ErrorOr<ICommand> Create(string[] args, IScope scope);
}