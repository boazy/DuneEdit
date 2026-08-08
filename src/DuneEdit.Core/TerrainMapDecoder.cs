using System.Buffers.Binary;

namespace DuneEdit.Core;

public static class TerrainMapDecoder
{
    public const int Width = 256;
    public const int Height = 151;
    public const int MinimumLatitude = -75;
    public const int MaximumLatitude = 75;
    public const int LatitudeRecordCount = 99;
    public const int LatitudeRecordSize = 8;
    public const int ExpectedMapLength = 50_681;

    // This subpixel longitude reproduces the field boundaries obtained from MAP2.HSQ.
    private const byte PixelLongitudeFraction = 0x24;


    public static byte[] DecodeRgba32(
        ReadOnlySpan<byte> compressedMap,
        ReadOnlySpan<byte> latitudeTable,
        ReadOnlySpan<byte> compressedPaletteResource) =>
        RenderRgba32(
            HsqCompression.Decompress(compressedMap),
            latitudeTable,
            CryoPaletteDecoder.DecodeRgb24(compressedPaletteResource));

    public static byte[] ProjectTerrainBytes(ReadOnlySpan<byte> map, ReadOnlySpan<byte> latitudeTable)
    {
        ValidateInputs(map, latitudeTable);
        var projected = GC.AllocateUninitializedArray<byte>(Width * Height);

        for (var row = 0; row < Height; row++)
        {
            var latitude = row + MinimumLatitude;
            for (var column = 0; column < Width; column++)
            {
                var longitude = (ushort)((column << 8) | PixelLongitudeFraction);
                projected[(row * Width) + column] = Sample(map, latitudeTable, longitude, latitude);
            }
        }

        return projected;
    }

    public static byte[] RenderRgba32(
        ReadOnlySpan<byte> map,
        ReadOnlySpan<byte> latitudeTable,
        ReadOnlySpan<byte> palette)
    {
        if (palette.Length != CryoPaletteDecoder.Rgb24Length)
        {
            throw new ArgumentException(
                $"The palette must contain exactly {CryoPaletteDecoder.Rgb24Length} RGB bytes.",
                nameof(palette));
        }

        var terrain = ProjectTerrainBytes(map, latitudeTable);
        var pixels = GC.AllocateUninitializedArray<byte>(terrain.Length * 4);

        for (var source = 0; source < terrain.Length; source++)
        {
            var paletteOffset = GetPaletteIndex(terrain[source]) * CryoPaletteDecoder.ComponentsPerColor;
            var destination = source * 4;
            pixels[destination] = palette[paletteOffset];
            pixels[destination + 1] = palette[paletteOffset + 1];
            pixels[destination + 2] = palette[paletteOffset + 2];
            pixels[destination + 3] = byte.MaxValue;
        }

        return pixels;
    }

    // On-disk MAP.HSQ values use the low nibble for terrain. The game displays it
    // through ONMAP.HSQ palette entries 0x10-0x1F.
    public static byte GetPaletteIndex(byte terrainValue) =>
        (byte)(0x10 + (terrainValue & 0x0F));

    public static byte Sample(
        ReadOnlySpan<byte> map,
        ReadOnlySpan<byte> latitudeTable,
        ushort longitude,
        int latitude)
    {
        ValidateInputs(map, latitudeTable);
        return map[GetSourceIndex(map.Length, latitudeTable, longitude, latitude)];
    }

    public static int GetSourceIndex(
        int mapLength,
        ReadOnlySpan<byte> latitudeTable,
        ushort longitude,
        int latitude)
    {
        if (mapLength != ExpectedMapLength)
        {
            throw new ArgumentException($"A decompressed MAP.HSQ must contain exactly {ExpectedMapLength} bytes.", nameof(mapLength));
        }

        if (latitude is < -98 or > 98)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "The latitude must be between -98 and 98.");
        }

        ValidateLatitudeTable(latitudeTable);
        var record = latitudeTable.Slice(Math.Abs(latitude) * LatitudeRecordSize, LatitudeRecordSize);
        var rowOffset = BinaryPrimitives.ReadUInt16BigEndian(record);
        var halfWidth = BinaryPrimitives.ReadUInt16BigEndian(record[2..]);
        var rowWidth = halfWidth * 2;
        var rowStart = ((mapLength + 1) / 2) + (latitude < 0 ? -rowOffset : rowOffset);
        var columnOffset = (rowWidth * (uint)longitude) >> 16;
        var sourceIndex = checked(rowStart + (int)columnOffset);

        if ((uint)sourceIndex >= mapLength)
        {
            throw new InvalidDataException("TABLAT.BIN points outside the decompressed map.");
        }

        return sourceIndex;
    }


    private static void ValidateInputs(ReadOnlySpan<byte> map, ReadOnlySpan<byte> latitudeTable)
    {
        if (map.Length != ExpectedMapLength)
        {
            throw new InvalidDataException($"A decompressed MAP.HSQ must contain exactly {ExpectedMapLength} bytes.");
        }

        ValidateLatitudeTable(latitudeTable);
    }

    private static void ValidateLatitudeTable(ReadOnlySpan<byte> latitudeTable)
    {
        if (latitudeTable.Length < LatitudeRecordCount * LatitudeRecordSize)
        {
            throw new InvalidDataException("TABLAT.BIN does not contain all latitude records.");
        }
    }
}
