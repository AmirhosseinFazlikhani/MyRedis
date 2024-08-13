using System.Text;

namespace Redis.Server;

public static class Persistence
{
    public static void Load(IClock clock)
    {
        var path = Path.Combine(Configuration.Directory, Configuration.DbFileName);

        if (!File.Exists(path))
        {
            return;
        }

        var file = File.OpenRead(path);
        using var reader = new BinaryReader(file);

        if (Encoding.UTF8.GetString(reader.ReadBytes(5)) != "REDIS")
        {
            throw new FormatException("Rdb file format is invalid.");
        }

        var version = int.Parse(Encoding.ASCII.GetString(reader.ReadBytes(4)));

        if (version != 9)
        {
            throw new NotSupportedException("Rdb file version is unsupported.");
        }

        while (reader.ReadByte() != 0xFE)
        {
        }

        var databaseNumber = ReadLength(reader);

        if (databaseNumber != 0)
        {
            throw new NotSupportedException("Multi database feature is unsupported.");
        }

        if (file.ReadByte() != 0xFB)
        {
            throw new FormatException("Rdb file format is invalid.");
        }

        var keyValueHashTableSize = ReadLength(reader);
        var keyExpiryHashTableSize = ReadLength(reader);

        int flag;

        while ((flag = file.ReadByte()) != 0xFF)
        {
            ReadKeyValuePair(reader, flag, clock);
        }
    }

    private static void ReadKeyValuePair(BinaryReader reader, int flag, IClock clock)
    {
        var expiry = default(DateTime);
        switch (flag)
        {
            case 0xFD:
                expiry = DateTime.UnixEpoch.AddSeconds(reader.ReadUInt32());
                flag = reader.ReadByte();
                break;
            case 0xFC:
                expiry = DateTime.UnixEpoch.AddMilliseconds(reader.ReadUInt64());
                flag = reader.ReadByte();
                break;
        }
        
        var key = DecodeString(reader);

        var value = flag switch
        {
            0 => DecodeString(reader),
            _ => throw new NotSupportedException("Value type is unsupported.")
        };
        
        if (expiry != default && expiry < clock.Now())
        {
            return;
        }

        DataStore.KeyValueStore[key] = value;

        if (expiry != default)
        {
            DataStore.KeyExpiryStore[key] = expiry;
        }
    }

    private static string DecodeString(BinaryReader reader)
    {
        var firstByte = reader.ReadByte();
        var length = firstByte & 0x3F;
        
        if (length == 0x3F)
        {
            length = ReadLength(reader);
        }

        var data = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(data);
    }

    private static int ReadLength(BinaryReader reader)
    {
        var firstByte = reader.ReadByte();
        
        if ((firstByte & 0x80) == 0)
        {
            // 6-bit length
            return firstByte;
        }

        if ((firstByte & 0xC0) == 0x80)
        {
            // 14-bit length
            var secondByte = reader.ReadByte();
            return ((firstByte & 0x3F) << 8) | secondByte;
        }

        // 32-bit length
        return reader.ReadInt32();
    }
}