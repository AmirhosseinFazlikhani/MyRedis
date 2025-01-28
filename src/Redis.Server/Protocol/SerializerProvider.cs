namespace Redis.Server.Protocol;

public static class SerializerProvider
{
    public static IResultSerializer Serializer { get; set; } = new Resp2Serializer();
}