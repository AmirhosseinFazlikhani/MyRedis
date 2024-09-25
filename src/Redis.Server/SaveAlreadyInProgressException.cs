namespace Redis.Server;

public class SaveAlreadyInProgressException : Exception
{
    public SaveAlreadyInProgressException() : base("Background save already in progress")
    {
    }
}