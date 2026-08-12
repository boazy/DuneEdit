using System.Buffers.Binary;

namespace DuneEdit.Core;

public static class CryoPaletteDecoder
{
    public const int ColorCount = 256;
    public const int ComponentsPerColor = 3;
    public const int Rgb24Length = ColorCount * ComponentsPerColor;

    public static byte[] DecodeRgb24(ReadOnlySpan<byte> compressedResource) =>
        DecodeRgb24FromResource(HsqCompression.Decompress(compressedResource));

    public static byte[] DecodeRgb24FromResource(ReadOnlySpan<byte> resource)
    {
        if (resource.Length < 4)
        {
            throw new InvalidDataException("The Cryo image resource is too short to contain a palette.");
        }

        var paletteEnd = BinaryPrimitives.ReadUInt16LittleEndian(resource);
        if (paletteEnd < 4 || paletteEnd > resource.Length)
        {
            throw new InvalidDataException("The Cryo image resource has an invalid palette boundary.");
        }

        var palette = new byte[Rgb24Length];
        var position = 2;
        while (position + 2 <= paletteEnd)
        {
            if (IsTerminator(resource, position))
            {
                return palette;
            }

            position = DecodeSubpalette(resource, paletteEnd, palette, position);
        }

        throw new InvalidDataException("The Cryo palette has no FF FF terminator.");
    }

    private static bool IsTerminator(ReadOnlySpan<byte> resource, int position) =>
        resource[position] == byte.MaxValue && resource[position + 1] == byte.MaxValue;

    private static int DecodeSubpalette(
        ReadOnlySpan<byte> resource,
        int paletteEnd,
        Span<byte> palette,
        int position)
    {
        var firstColor = resource[position++];
        var colorCount = resource[position++];
        if (firstColor + colorCount > ColorCount)
        {
            throw new InvalidDataException("A Cryo subpalette extends beyond color index 255.");
        }

        var componentCount = checked(colorCount * ComponentsPerColor);
        if (position > paletteEnd - componentCount)
        {
            throw new InvalidDataException("A Cryo subpalette extends beyond the palette boundary.");
        }

        var destination = firstColor * ComponentsPerColor;
        for (var component = 0; component < componentCount; component++)
        {
            palette[destination + component] = DecodeComponent(resource[position++]);
        }

        return position;
    }

    private static byte DecodeComponent(byte value)
    {
        if (value > 0x3F)
        {
            throw new InvalidDataException("A Cryo palette component exceeds the VGA 6-bit range.");
        }

        return ExpandVgaComponent(value);
    }

    private static byte ExpandVgaComponent(byte value) =>
        (byte)((value << 2) | (value >> 4));
}
