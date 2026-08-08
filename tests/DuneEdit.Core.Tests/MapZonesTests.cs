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

    [Fact]
    public void RejectsLatitudeOutsideGameMap()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapZones.GetSpiceField(0, 100));
    }
}
