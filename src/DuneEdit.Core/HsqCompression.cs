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

        var output = GC.AllocateUninitializedArray<byte>(
            BinaryPrimitives.ReadUInt16LittleEndian(compressed));
        var reader = new HsqReader(compressed);
        var destination = 0;

        while (true)
        {
            var instruction = reader.ReadInstruction();
            switch (instruction.Kind)
            {
                case HsqInstructionKind.Literal:
                    EnsureDestination(output, destination, 1);
                    output[destination++] = instruction.Literal;
                    break;

                case HsqInstructionKind.BackReference:
                    CopyBackReference(output, ref destination, instruction);
                    break;

                case HsqInstructionKind.End:
                    EnsureCompleteOutput(destination, output.Length);
                    return output;

                default:
                    throw new InvalidDataException("The HSQ stream contains an unknown instruction.");
            }
        }
    }

    private static void CopyBackReference(
        Span<byte> output,
        ref int destination,
        HsqInstruction instruction)
    {
        EnsureDestination(output, destination, instruction.Count);
        var copySource = destination + instruction.Offset;
        if (copySource < 0)
        {
            throw new InvalidDataException("The HSQ stream contains an invalid back-reference.");
        }

        for (var index = 0; index < instruction.Count; index++)
        {
            output[destination++] = output[copySource++];
        }
    }

    private static void EnsureCompleteOutput(int actualLength, int declaredLength)
    {
        if (actualLength != declaredLength)
        {
            throw new InvalidDataException("The HSQ stream ended before its declared output length.");
        }
    }

    private enum HsqInstructionKind
    {
        Literal,
        BackReference,
        End,
    }

    private readonly record struct HsqInstruction(
        HsqInstructionKind Kind,
        byte Literal,
        int Offset,
        int Count)
    {
        public static HsqInstruction FromLiteral(byte value) =>
            new(HsqInstructionKind.Literal, value, 0, 0);

        public static HsqInstruction FromBackReference(int offset, int count) =>
            new(HsqInstructionKind.BackReference, 0, offset, count);

        public static HsqInstruction End => new(HsqInstructionKind.End, 0, 0, 0);
    }

    private ref struct HsqReader
    {
        private readonly ReadOnlySpan<byte> data;
        private int source = HeaderLength;
        private int control = 1;

        public HsqReader(ReadOnlySpan<byte> data)
        {
            this.data = data;
        }

        public HsqInstruction ReadInstruction()
        {
            if (ReadBit() != 0)
            {
                return HsqInstruction.FromLiteral(ReadByte());
            }

            return ReadBit() != 0
                ? ReadLongBackReference()
                : ReadShortBackReference();
        }

        private HsqInstruction ReadLongBackReference()
        {
            var encoded = ReadUInt16();
            var count = encoded & 0x07;
            var offset = (encoded >> 3) - 0x2000;
            if (count != 0)
            {
                return HsqInstruction.FromBackReference(offset, count + 2);
            }

            var extendedCount = ReadByte();
            return extendedCount == 0
                ? HsqInstruction.End
                : HsqInstruction.FromBackReference(offset, extendedCount + 2);
        }

        private HsqInstruction ReadShortBackReference()
        {
            var count = (ReadBit() << 1) | ReadBit();
            var offset = ReadByte() - 0x100;
            return HsqInstruction.FromBackReference(offset, count + 2);
        }

        private int ReadBit()
        {
            if (control == 1)
            {
                control = 0x10000 | ReadUInt16();
            }

            var bit = control & 1;
            control >>= 1;
            return bit;
        }

        private ushort ReadUInt16()
        {
            EnsureAvailable(2);
            var value = BinaryPrimitives.ReadUInt16LittleEndian(data[source..]);
            source += 2;
            return value;
        }

        private byte ReadByte()
        {
            EnsureAvailable(1);
            return data[source++];
        }

        private void EnsureAvailable(int length)
        {
            if (source < 0 || length < 0 || source > data.Length - length)
            {
                throw new InvalidDataException("The HSQ stream ends inside an instruction.");
            }
        }
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


    private static void EnsureDestination(ReadOnlySpan<byte> destination, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > destination.Length - length)
        {
            throw new InvalidDataException("The HSQ stream exceeds its declared output length.");
        }
    }
}
