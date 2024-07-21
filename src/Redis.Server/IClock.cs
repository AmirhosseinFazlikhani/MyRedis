namespace Redis.Server;

public interface IClock
{
    DateTime Now();
}