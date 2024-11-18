using System.Text;

namespace Redis.Server.Persistence;

public static class RdbFile
{
    private const byte EndMetadataFlag = 0xFE;
    private const byte StartDbFlag = 0xFB;
    private const byte EndDbFlag = 0xFF;
    private const byte StringValueFlag = 0;
    private const byte ExpiryInSecondsFlag = 0xFD;
    private const byte ExpiryInMillisecondsFlag = 0xFC;
    private const int DbNumber = 0;
    private const short Version = 10;

    private static readonly byte[] Header = "REDIS"u8.ToArray();
    private static Task _saveTask = Task.CompletedTask;
    public static bool SaveInProgress => _saveTask.Status == TaskStatus.Running;

    public static DateTime LastSaveDateTime { get; private set; }

    public static Task SaveAsync(IClock clock,
        Dictionary<string, string> keyValueStore,
        Dictionary<string, DateTime> keyExpiryStore)
    {
        if (_saveTask.IsCompleted)
        {
            _saveTask = Task.Run(() => Save(clock, keyValueStore, keyExpiryStore));
        }

        return _saveTask;
    }

    private static void Save(IClock clock,
        Dictionary<string, string> keyValueStore,
        Dictionary<string, DateTime> keyExpiryStore)
    {
        var filePath = GetDbFilePath();
        var tempFilePath = filePath + ".new";
        var tempFile = File.OpenWrite(tempFilePath);

        using (var writer = new BinaryWriter(tempFile, leaveOpen: false, encoding: new UTF8Encoding(false)))
        {
            writer.Write(Header);
            writer.Write(Encoding.UTF8.GetBytes(Version.ToString("0000")));
            writer.Write(EndMetadataFlag);
            WriteLength(writer, DbNumber);
            writer.Write(StartDbFlag);
            WriteLength(writer, keyValueStore.Count);
            WriteLength(writer, keyExpiryStore.Count);
            WriteDataStore(writer, clock, keyValueStore, keyExpiryStore);
            writer.Write(EndDbFlag);
        }

        File.Delete(filePath);
        File.Move(tempFilePath, filePath);

        LastSaveDateTime = clock.Now();
    }

    public static void Load(IClock clock, string? path = null)
    {
        path ??= GetDbFilePath();

        if (!File.Exists(path))
        {
            return;
        }

        var file = File.OpenRead(path);
        using var reader = new BinaryReader(file);

        if (!reader.ReadBytes(5).SequenceEqual(Header))
        {
            throw new FormatException("Rdb file format is invalid.");
        }

        if (short.Parse(reader.ReadBytes(4)) > Version)
        {
            throw new NotSupportedException("Rdb file version is unsupported.");
        }

        while (reader.ReadByte() != EndMetadataFlag)
        {
        }

        var databaseNumber = ReadLength(reader);

        if (databaseNumber != DbNumber)
        {
            throw new NotSupportedException("Multi database feature is unsupported.");
        }

        if (file.ReadByte() != StartDbFlag)
        {
            throw new FormatException("Rdb file format is invalid.");
        }

        // KeyValueHashTableSize
        _ = ReadLength(reader);

        // KeyExpiryHashTableSize
        _ = ReadLength(reader);

        int flag;

        while ((flag = file.ReadByte()) != EndDbFlag)
        {
            ReadKeyValuePair(reader, ref flag, clock);
        }
    }

    private static string GetDbFilePath() => Path.Combine(Configuration.Directory, Configuration.DbFileName);

    private static void ReadKeyValuePair(BinaryReader reader, ref int flag, IClock clock)
    {
        var expiry = default(DateTime);
        switch (flag)
        {
            case ExpiryInSecondsFlag:
                expiry = DateTime.UnixEpoch.AddSeconds(reader.ReadUInt32());
                flag = reader.ReadByte();
                break;
            case ExpiryInMillisecondsFlag:
                expiry = DateTime.UnixEpoch.AddMilliseconds(reader.ReadUInt64());
                flag = reader.ReadByte();
                break;
        }

        var key = ReadString(reader);

        var value = flag switch
        {
            StringValueFlag => ReadString(reader),
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

    private static void WriteDataStore(BinaryWriter writer,
        IClock clock,
        Dictionary<string, string> keyValueStore,
        Dictionary<string, DateTime> keyExpiryStore)
    {
        foreach (var keyValuePair in keyValueStore)
        {
            if (keyExpiryStore.TryGetValue(keyValuePair.Key, out var expiry))
            {
                if (expiry <= clock.Now())
                {
                    continue;
                }

                var expiryInUnixTimestamp = (uint)(expiry - DateTime.UnixEpoch).TotalSeconds;
                writer.Write(ExpiryInSecondsFlag);
                writer.Write(expiryInUnixTimestamp);
            }

            writer.Write(StringValueFlag);
            WriteString(writer, keyValuePair.Key);
            WriteString(writer, keyValuePair.Value);
        }
    }

    private static string ReadString(BinaryReader reader)
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

    private static void WriteString(BinaryWriter writer, string value)
    {
        var data = Encoding.UTF8.GetBytes(value);
        var length = data.Length;

        if (length < 0x3F)
        {
            writer.Write((byte)length);
        }
        else
        {
            writer.Write((byte)0x3F);
            WriteLength(writer, length);
        }

        writer.Write(data);
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

    private static void WriteLength(BinaryWriter writer, int value)
    {
        if (value < 0x80)
        {
            // 6-bit length
            writer.Write((byte)value);
        }
        else if (value < 0x4000)
        {
            // 14-bit length
            writer.Write((byte)((value >> 8) | 0x80)); // Set the first two bits
            writer.Write((byte)(value & 0xFF)); // Write the remaining 8 bits
        }
        else
        {
            // 32-bit length
            writer.Write((byte)0xC0); // Set the first two bits
            writer.Write(value);
        }
    }
}