using DuneEdit.Core;

namespace DuneEdit.Core.Tests;

public sealed class CryoPaletteDecoderTests
{
    [Fact]
    public void DecodesSubpaletteIntoIndexedRgbSlots()
    {
        byte[] resource =
        [
            0x0C, 0x00,
            0x01, 0x02,
            0x00, 0x3F, 0x20,
            0x10, 0x08, 0x04,
            0xFF, 0xFF,
        ];

        var palette = CryoPaletteDecoder.DecodeRgb24FromResource(resource);

        Assert.Equal([0, 0, 0], palette.AsSpan(0, 3).ToArray());
        Assert.Equal([0, 255, 130], palette.AsSpan(3, 3).ToArray());
        Assert.Equal([65, 32, 16], palette.AsSpan(6, 3).ToArray());
        Assert.Equal(CryoPaletteDecoder.Rgb24Length, palette.Length);
    }

    [Fact]
    public void RejectsPaletteWithoutTerminator()
    {
        byte[] resource = [0x04, 0x00, 0x01, 0x00];

        var error = Assert.Throws<InvalidDataException>(() =>
            CryoPaletteDecoder.DecodeRgb24FromResource(resource));

        Assert.Contains("terminator", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsComponentOutsideVgaRange()
    {
        byte[] resource =
        [
            0x07, 0x00,
            0x01, 0x01,
            0x40, 0x00, 0x00,
        ];

        var error = Assert.Throws<InvalidDataException>(() =>
            CryoPaletteDecoder.DecodeRgb24FromResource(resource));

        Assert.Contains("6-bit range", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSubpaletteBeyondColorTable()
    {
        byte[] resource = [0x06, 0x00, 0xFF, 0x02, 0xFF, 0xFF];

        var error = Assert.Throws<InvalidDataException>(() =>
            CryoPaletteDecoder.DecodeRgb24FromResource(resource));

        Assert.Contains("color index 255", error.Message, StringComparison.Ordinal);
    }
}
