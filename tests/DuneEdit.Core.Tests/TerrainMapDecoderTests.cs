using System.Security.Cryptography;
using DuneEdit.Core;

namespace DuneEdit.Core.Tests;

public sealed class TerrainMapDecoderTests
{
    private const string OxtynLatitudeRowBase64 =
        "CwsLBwcHBwQEBAQEBAQJCAkJCQYGBgQGBgcFBQUHBwYHBgcGBgYGBwcHBgYJBwgHBwcICAkJCAgJBwcH" +
        "BwcGBwcEBgQGBAQGBAQEBAQEBAQEBAQGBAQGBgQEBAQGBAQEBgQEBgQEBAcEBAQEBAQEBAQEBAQEBAQE" +
        "BAQEBAQHCgoMDA0NDQwNDQ0NDQ4NDQwNCgoKCAcEBgcEBAYEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE" +
        "BAQEBAQEBAQEBAQEBAQEBAQEBAQEBwcICQkEBAQEBAQEBAQEBAQEBAkJBAQEBgQEBQUFBQQEBAQHBwQE" +
        "BgYGBgYGCAgJCQsLCgsLCwsLCgoKCgoKCgoKCgoKCgkIBwgJCwsJCQgEBAQECQoKCgoNDQ0NDQ0NDQkH" +
        "BwQEBAQECAgJDAoKCQkJCAgJCAcHBAQHBwcEBwcEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBgYEBAYEBAQE" +
        "BAQICAkLCgwNDQ0NCwoLDQ0NDAwMDQ==";

    private const string OnMapPaletteAreaBase64 =
        "LgIAAQAAAAIBACoABgYqFQAqKioVFRUVFT8VPxUVPz8PAT8/PxQMPjkbPz8mPz0VPzYbPy0ANiQKOBgKLRAAJBIAGxIPGw4IEgAPUBAIAwAPCQASCwAWDgAZEAAdEwAgFgAjGQAnHAAqIAAtJAAxKAA0LAA4MAA7NQA/OgBhFTgAADAAACwAACAAABgAAA4KDBgPFCEYHSsjJzUwMz8/PwMGDwoPGxMbKCEpNQ4PDw4NDjooADoKAAAsABUUE3hfFAgIHAwMIBAQKBgYMCAgNCQkOCwsPDAwAAAADw8PFxcXHw4AMBk4MwsVJw0aHwEIEwAACAAAAwYPBwwYDhQiFx4rISk1LjU/Fio/AAAADw8PFxcXHx8fJycnLy8vNzc3Pz8/Pz8EOjEAMSQAKx4AHxIAFAAAPxIRPyUaAD8AAC8AACcAAB8AABcAAAAAAAAACQMBFAgCHgwEKREGLxcJNR4OOyYTOjEAMSQAKx4AHxIAFAAAAAAAFAAAHAwIJAAAMAQAMBMVMwsQPBAEHBAAOhgULzE2AQUIPA8QCAwQAQsUABEYDhUcDBskDCIoIDg8HgAAIwEBKQICLgUFNAgIPDgwICk/FiE3DRsvBxYnAhIfPy8bNyMPLxcGJw4A4QoAAAARAAAVAAAZAAAdCAEiCQAnDgAsEQAwFgUyGgjwDwMHDAUKEAcNFAoRGQ0VHREZIhQcJRcfKRoiLB0lLyEpMiUsNSkwOC41OzM5P///";

    [Fact]
    public void ProjectsActualOxtynLatitudeRow()
    {
        var latitudeTable = new byte[TerrainMapDecoder.LatitudeRecordCount * TerrainMapDecoder.LatitudeRecordSize];
        Convert.FromHexString("1BB000BF00F50000").CopyTo(
            latitudeTable,
            0x12 * TerrainMapDecoder.LatitudeRecordSize);

        var map = new byte[TerrainMapDecoder.ExpectedMapLength];
        var oxtynLatitudeRow = Convert.FromBase64String(OxtynLatitudeRowBase64);
        Assert.Equal(382, oxtynLatitudeRow.Length);
        oxtynLatitudeRow.CopyTo(map, 32_429);

        var projected = TerrainMapDecoder.ProjectTerrainBytes(map, latitudeTable);
        var projectedOxtynLatitude = projected.AsSpan(
            (0x12 - TerrainMapDecoder.MinimumLatitude) * TerrainMapDecoder.Width,
            TerrainMapDecoder.Width);

        Assert.Equal(
            "5dce9e8098325f3e2fed53eb4b768e6fe9b37cde76823f77c56ceb664f720ab6",
            Convert.ToHexString(SHA256.HashData(projectedOxtynLatitude)).ToLowerInvariant());
        Assert.Equal(
            32_453,
            TerrainMapDecoder.GetSourceIndex(
                map.Length,
                latitudeTable,
                longitude: 0x10C1,
                latitude: 0x12));
        Assert.Equal(
            0x06,
            TerrainMapDecoder.Sample(map, latitudeTable, longitude: 0x10C1, latitude: 0x12));
    }

    [Fact]
    public void UsesOnMapPaletteAndTerrainLowNibble()
    {
        var paletteResource = Convert.FromBase64String(OnMapPaletteAreaBase64);
        var palette = CryoPaletteDecoder.DecodeRgb24FromResource(paletteResource);

        Assert.Equal([251, 231, 109], palette.AsSpan(0x14 * 3, 3).ToArray());
        Assert.Equal([255, 247, 85], palette.AsSpan(0x16 * 3, 3).ToArray());
        Assert.Equal([73, 0, 60], palette.AsSpan(0x1F * 3, 3).ToArray());
        Assert.Equal(0x16, TerrainMapDecoder.GetPaletteIndex(0x06));
        Assert.Equal(0x16, TerrainMapDecoder.GetPaletteIndex(0x36));
    }

    [Fact]
    public void DecompressesCryoHsqLiteralStream()
    {
        byte[] compressed =
        [
            0x03, 0x00, 0x00, 0x00, 0x00, 0xA8,
            0x17, 0x00,
            (byte)'A', (byte)'B', (byte)'C',
            0x00, 0x00, 0x00,
        ];

        Assert.Equal("ABC"u8.ToArray(), HsqCompression.Decompress(compressed));
    }
}
