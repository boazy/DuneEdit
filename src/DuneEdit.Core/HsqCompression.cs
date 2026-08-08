using System.Buffers.Binary;

namespace DuneEdit.Core;

public static class HsqCompression
{
    private const int HeaderLength = 6;

    public static byte[] Decompress(ReadOnlySpan<byte> compressed)
    {
        if (compressed.Length < HeaderLength || SumHeader(compressed) != 0xAB)
        {
            throw new InvalidDataException("The HSQ header is invalid.");
        }

        var output = GC.AllocateUninitializedArray<byte>(BinaryPrimitives.ReadUInt16LittleEndian(compressed));
        var source = HeaderLength;
        var destination = 0;
        var control = 1;

        while (true)
        {
            if (ReadBit(compressed, ref source, ref control) != 0)
            {
                EnsureSource(compressed, source, 1);
                EnsureDestination(output, destination, 1);
                output[destination++] = compressed[source++];
                continue;
            }

            int count;
            int offset;
            if (ReadBit(compressed, ref source, ref control) != 0)
            {
                EnsureSource(compressed, source, 2);
                var encoded = BinaryPrimitives.ReadUInt16LittleEndian(compressed[source..]);
                source += 2;
                count = encoded & 0x07;
                offset = (encoded >> 3) - 0x2000;

                if (count == 0)
                {
                    EnsureSource(compressed, source, 1);
                    count = compressed[source++];
                    if (count == 0)
                    {
                        if (destination != output.Length)
                        {
                            throw new InvalidDataException("The HSQ stream ended before its declared output length.");
                        }

                        return output;
                    }
                }
            }
            else
            {
                count = (ReadBit(compressed, ref source, ref control) << 1)
                    | ReadBit(compressed, ref source, ref control);
                EnsureSource(compressed, source, 1);
                offset = compressed[source++] - 0x100;
            }

            count += 2;
            EnsureDestination(output, destination, count);
            var copySource = destination + offset;
            if (copySource < 0)
            {
                throw new InvalidDataException("The HSQ stream contains an invalid back-reference.");
            }

            for (var index = 0; index < count; index++)
            {
                output[destination++] = output[copySource++];
            }
        }
    }

    private static int ReadBit(ReadOnlySpan<byte> sourceData, ref int source, ref int control)
    {
        if (control == 1)
        {
            EnsureSource(sourceData, source, 2);
            control = 0x10000 | BinaryPrimitives.ReadUInt16LittleEndian(sourceData[source..]);
            source += 2;
        }

        var bit = control & 1;
        control >>= 1;
        return bit;
    }

    private static int SumHeader(ReadOnlySpan<byte> compressed)
    {
        var sum = 0;
        for (var index = 0; index < HeaderLength; index++)
        {
            sum += compressed[index];
        }

        return sum & byte.MaxValue;
    }

    private static void EnsureSource(ReadOnlySpan<byte> source, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > source.Length - length)
        {
            throw new InvalidDataException("The HSQ stream ends inside an instruction.");
        }
    }

    private static void EnsureDestination(ReadOnlySpan<byte> destination, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > destination.Length - length)
        {
            throw new InvalidDataException("The HSQ stream exceeds its declared output length.");
        }
    }
}
