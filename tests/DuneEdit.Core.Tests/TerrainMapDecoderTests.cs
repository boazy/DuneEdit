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

    private const string LatitudeMinus75RowBase64 =
        "Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc5CQkICAgIDAwMDDo6Ojo6Ojo5OTk5Ojo7Ozs7Ozo6PDw8PDw6PDsL" +
        "CwsLCQkJCQkJCQkJCQkJCQkKCgwKCwoKCggICQwMDQ0MDAoKCgsMCwsLDAwKCgoKDAwMDAoKCgwKCgoK" +
        "CggICAgEBAQEBAQEBAQEBAQEBAQEBAQHBzc3AA==";

    private const string LatitudeMinus47RowBase64 =
        "NjY2NjU1NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY0NDQ0NDQ0NDQ0NDQ0NDU0NTU2NjQ2NTQ0NDQ0" +
        "NDc0NDQ0NDQ0NDQ0NDQ0NDQ0NTU5PDo6PDo5ODg2NTU1NDQ0NDQ0NDQ0NTQ2NjY1NTQ0NDQ0NDQ0NDQ0" +
        "NDQ0NDQ0NDQ0NDQ0NDQ0NDU0NDY2NjY3NjY4ODoICgoLCw0NDAwMCwsLCwsLCwsLCwsKCgsLCwsLCAgI" +
        "CAgHCAgHBwQEBwcHBAQEBAQEBgQGBAQEBwQGBAQHBwQECQkJCjw8Ojo6Ozs7Ozg4ODo6ODg2Njk5OTc3" +
        "OTk3NzY2NjY3Nzc3NjY4ODc4Nzo6PDo6ODg4ODY2NjY0NDQ2Nzc3NDQ0NDQ0NDQ0NDQ0NDQA";

    private const string LatitudeMinus46RowBase64 =
        "NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY0NDQ0NDQ0NDQ0NDQ0NTU1NDU1NjY0NDQ0NDQ0" +
        "NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NTU5PDo6PTk5ODg2NTU0NDQ0NDQ0NDQ0NDQ1NTY2NTU1NDQ0NDQ0NDQ0" +
        "NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NTQ0NTY1Njc3Njg4OAoKDAsLDAwNDQwLCwoLCwsLCwsMDAwLCwsLCwsL" +
        "CwsLCwsJBwgHBwcHBwQEBwcEBAQEBAQEBwYGBAQGBAQHBwQEBAQJCgo8PDs7Ozo6Ojs7Ojo6Ojo5Ozs5" +
        "OTk5OTk2NjY2Nzc3NjY2Njc5ODg4CgoMDAo6ODg4ODc2NjY2NDQ0Njc2Nzc3NjY2NjY2NjY2NjY2AA==";

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
    public void JoinsActualNorthTerrainAcrossAntimeridian()
    {
        var latitudeTable = new byte[TerrainMapDecoder.LatitudeRecordCount * TerrainMapDecoder.LatitudeRecordSize];
        var map = new byte[TerrainMapDecoder.ExpectedMapLength];
        AddLatitudeFixture(latitudeTable, map, -75, "5BAC004A005F0000", 1_873, LatitudeMinus75RowBase64);
        AddLatitudeFixture(latitudeTable, map, -47, "42D8009300BD0000", 8_229, LatitudeMinus47RowBase64);
        AddLatitudeFixture(latitudeTable, map, -46, "41AE009500BF0000", 8_527, LatitudeMinus46RowBase64);

        var projected = TerrainMapDecoder.ProjectTerrainBytes(map, latitudeTable);

        AssertRowEdge(projected, row: 0, expectedTerrain: 0x37);
        AssertRowEdge(projected, row: 28, expectedTerrain: 0x34);
        AssertRowEdge(projected, row: 29, expectedTerrain: 0x36);
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

    private static void AddLatitudeFixture(
        byte[] latitudeTable,
        byte[] map,
        int latitude,
        string recordHex,
        int rowStart,
        string rowBase64)
    {
        Convert.FromHexString(recordHex).CopyTo(
            latitudeTable,
            Math.Abs(latitude) * TerrainMapDecoder.LatitudeRecordSize);
        Convert.FromBase64String(rowBase64).CopyTo(map, rowStart);
    }

    private static void AssertRowEdge(byte[] projected, int row, byte expectedTerrain)
    {
        var lastCell = ((row + 1) * TerrainMapDecoder.Width) - 1;
        Assert.Equal(expectedTerrain, projected[lastCell]);
        Assert.Equal(projected[lastCell - 1], projected[lastCell]);
    }
}
