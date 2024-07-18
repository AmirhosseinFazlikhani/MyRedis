namespace Redis.Server;

public class Entry
{
    public Entry(string value)
    {
        _value = value;
    }

    private readonly string _value;
    public string? GetValue() => IsExpired() ? null : _value;

    public DateTime? Expiry { get; set; }

    public bool IsExpired() => Expiry.HasValue && Expiry.Value < DateTime.UtcNow;
}