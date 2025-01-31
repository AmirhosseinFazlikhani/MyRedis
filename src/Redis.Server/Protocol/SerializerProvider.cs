namespace Redis.Server.Protocol;

public static class SerializerProvider
{
    public static IResultSerializer DefaultSerializer { get; set; } = new Resp2Serializer();
}