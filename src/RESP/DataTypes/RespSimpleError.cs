﻿namespace RESP.DataTypes;

public record RespSimpleError(string Value) : IRespData
{
    public const char Prefix = '-';
}