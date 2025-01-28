namespace Redis.Server.CommandDispatching;

public class SynchronizerAlreadyStartedOnceException : Exception
{
    public SynchronizerAlreadyStartedOnceException() : base(
        "CommandSynchronizer can only be started once during the lifetime of the application.")
    {
    }
}