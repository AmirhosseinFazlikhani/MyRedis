namespace RESP;

public static class DataTypes
{
    public const byte SimpleString = (byte)'+';
    public const byte SimpleError = (byte)'-';
    public const byte Integer = (byte)':';
    public const byte BulkString = (byte)'$';
    public const byte Array = (byte)'*';
    public const byte Null = (byte)'_';
    public const byte Boolean = (byte)'#';
    public const byte Double = (byte)',';
    public const byte BigNumber = (byte)'(';
    public const byte BulkError = (byte)'!';
    public const byte VerbatimString = (byte)'=';
    public const byte Map = (byte)'%';
    public const byte Set = (byte)'~';
    public const byte Push = (byte)'>';
}