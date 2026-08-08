using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

internal static class PngWriter
{
    private static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];

    public static void WriteRgba32(string path, int width, int height, ReadOnlySpan<byte> pixels)
    {
        if (pixels.Length != checked(width * height * 4))
        {
            throw new ArgumentException("The pixel buffer length does not match the image dimensions.", nameof(pixels));
        }

        using var output = File.Create(path);
        output.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(output, "IHDR", header);

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            var stride = width * 4;
            for (var row = 0; row < height; row++)
            {
                zlib.WriteByte(0);
                zlib.Write(pixels.Slice(row * stride, stride));
            }
        }

        WriteChunk(output, "IDAT", compressed.GetBuffer().AsSpan(0, checked((int)compressed.Length)));
        WriteChunk(output, "IEND", []);
    }

    private static void WriteChunk(Stream output, string name, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);

        Span<byte> type = stackalloc byte[4];
        Encoding.ASCII.GetBytes(name, type);
        output.Write(type);
        output.Write(data);

        var crc = uint.MaxValue;
        crc = UpdateCrc(crc, type);
        crc = UpdateCrc(crc, data);
        Span<byte> checksum = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(checksum, ~crc);
        output.Write(checksum);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1));
            }
        }

        return crc;
    }
}
