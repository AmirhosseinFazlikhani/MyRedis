namespace Redis.Server.CommandDispatching;

public static class CommandFactoryHelper
{
    public static bool StartWith(this string[] source, string value)
    {
        return source.Length != 0 && source[0].Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    public static bool StartWith(this string[] source, string[] value)
    {
        return source.Length >= value.Length &&
            !value.Where((t, i) => !t.Equals(source[i], StringComparison.OrdinalIgnoreCase)).Any();
    }
}