namespace Redis.Server;

public class Entry
{
    public Entry(string value)
    {
        Value = value;
    }

    public string Value { get; }
    public DateTime? Expiry { get; set; }

    public bool IsExpired(IClock clock) => Expiry.HasValue && Expiry.Value < clock.Now();
}