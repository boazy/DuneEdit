using DuneEdit.Core;

namespace DuneEdit.Core.Tests;

public sealed class MapZonesTests
{
    [Theory]
    [InlineData(0x19, 0xFC, 0x01)]
    [InlineData(0xF0, 0xDD, 0x11)]
    [InlineData(0x0F, 0x46, 0x16)]
    [InlineData(0xC0, 0x47, 0x43)]
    public void UsesOriginalGameFieldBoundaries(byte mapX, byte mapY, byte expectedField)
    {
        Assert.Equal(expectedField, MapZones.GetSpiceField(mapX, mapY));
    }

    [Theory]
    [InlineData(0, 0x15)]
    [InlineData(6, 0x18)]
    [InlineData(20, 0x18)]
    public void JoinsNorthCapShapesAcrossAntimeridian(int row, byte expectedField)
    {
        var cells = MapZones.Cells;
        Assert.Equal(expectedField, cells[(row * MapZones.Width) + (MapZones.Width - 1)]);
        Assert.Equal(expectedField, cells[row * MapZones.Width]);
    }

    [Fact]
    public void RejectsLatitudeOutsideGameMap()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapZones.GetSpiceField(0, 100));
    }
}
