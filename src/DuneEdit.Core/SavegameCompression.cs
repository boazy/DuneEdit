namespace DuneEdit.Core;

public static class SavegameCompression
{
    public const byte Marker = 0xF7;
    public const int HeaderLength = 6;

    public static byte[] Decompress(ReadOnlySpan<byte> data)
    {
        ValidateHeader(data);

        using var output = new MemoryStream(data.Length);
        output.Write(data[..HeaderLength]);

        for (var index = HeaderLength; index < data.Length; index++)
        {
            var value = data[index];
            if (value != Marker)
            {
                output.WriteByte(value);
                continue;
            }

            if (index + 2 >= data.Length)
            {
                throw new InvalidDataException("The save ends inside an F7 compression sequence.");
            }

            var count = data[index + 1];
            var repeatedValue = data[index + 2];
            for (var repeat = 0; repeat < count; repeat++)
            {
                output.WriteByte(repeatedValue);
            }

            index += 2;
        }

        return output.ToArray();
    }

    public static byte[] Compress(ReadOnlySpan<byte> data)
    {
        ValidateHeader(data);

        using var output = new MemoryStream(data.Length);
        output.Write(data[..HeaderLength]);

        var index = HeaderLength;
        while (index < data.Length)
        {
            var value = data[index];
            var count = 1;
            while (index + count < data.Length && data[index + count] == value && count < byte.MaxValue)
            {
                count++;
            }

            WriteRun(output, value, count);
            index += count;
        }

        var compressed = output.ToArray();
        var encodedLength = compressed.Length - 2;
        if (encodedLength > ushort.MaxValue)
        {
            throw new InvalidDataException("The compressed save is too large for Dune's 16-bit length header.");
        }

        compressed[4] = (byte)encodedLength;
        compressed[5] = (byte)(encodedLength >> 8);
        return compressed;
    }

    public static int ReadDeclaredFileLength(ReadOnlySpan<byte> data)
    {
        ValidateHeader(data);
        return (data[4] | (data[5] << 8)) + 2;
    }

    private static void WriteRun(Stream output, byte value, int count)
    {
        if (value == Marker || count > 2)
        {
            output.WriteByte(Marker);
            output.WriteByte((byte)count);
            output.WriteByte(value);
            return;
        }

        for (var repeat = 0; repeat < count; repeat++)
        {
            output.WriteByte(value);
        }
    }

    private static void ValidateHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderLength)
        {
            throw new InvalidDataException($"A Dune save must contain at least {HeaderLength} header bytes.");
        }
    }
}
