namespace Redis.Server;

public class Entry
{
    public Entry(string value)
    {
        _value = value;
    }

    private readonly string _value;
    public string? GetValue(IClock clock) => IsExpired(clock) ? null : _value;

    public DateTime? Expiry { get; set; }

    public bool IsExpired(IClock clock) => Expiry.HasValue && Expiry.Value < clock.Now();
}