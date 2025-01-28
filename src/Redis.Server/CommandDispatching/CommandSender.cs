namespace Redis.Server.CommandDispatching;

public record CommandSender
{
    public int? ClientId { get; init; }
    public string? ClientName { get; init; }
}