namespace Redis.Server;

public class Clock : IClock
{
    public DateTime Now()
    {
        return DateTime.UtcNow;
    }
}