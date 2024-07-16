namespace RESP;

public static class DataTypes
{
    public const char SimpleString = '+';
    public const char SimpleError = '-';
    public const char Integer = ':';
    public const char BulkString = '$';
    public const char Array = '*';
    public const char Null = '_';
    public const char Boolean = '#';
    public const char Double = ',';
    public const char BigNumber = '(';
    public const char BulkError = '!';
    public const char VerbatimString = '=';
    public const char Map = '%';
    public const char Set = '~';
    public const char Push = '>';
}