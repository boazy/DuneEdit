using DuneEdit.Core;

namespace DuneEdit.Core.Tests;

public sealed class HsqCompressionTests
{
    [Fact]
    public void DecompressesOverlappingShortBackReference()
    {
        byte[] compressed =
        [
            0x03, 0x00, 0x00, 0x00, 0x00, 0xA8,
            0x41, 0x00,
            (byte)'A',
            0xFF,
            0x00, 0x00, 0x00,
        ];

        Assert.Equal("AAA"u8.ToArray(), HsqCompression.Decompress(compressed));
    }

    [Fact]
    public void DecompressesLongBackReference()
    {
        byte[] compressed =
        [
            0x08, 0x00, 0x00, 0x00, 0x00, 0xA3,
            0x57, 0x00,
            (byte)'A', (byte)'B', (byte)'C',
            0xEB, 0xFF,
            0x00, 0x00, 0x00,
        ];

        Assert.Equal("ABCABCAB"u8.ToArray(), HsqCompression.Decompress(compressed));
    }

    [Fact]
    public void DecompressesExtendedLongBackReference()
    {
        byte[] compressed =
        [
            0x06, 0x00, 0x00, 0x00, 0x00, 0xA5,
            0x15, 0x00,
            (byte)'A',
            0xF8, 0xFF, 0x03,
            0x00, 0x00, 0x00,
        ];

        Assert.Equal("AAAAAA"u8.ToArray(), HsqCompression.Decompress(compressed));
    }

    [Fact]
    public void RejectsBackReferenceBeforeOutputStart()
    {
        byte[] compressed =
        [
            0x03, 0x00, 0x00, 0x00, 0x00, 0xA8,
            0x02, 0x00,
            0xF1, 0xFF,
        ];

        var error = Assert.Throws<InvalidDataException>(() => HsqCompression.Decompress(compressed));

        Assert.Contains("invalid back-reference", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTerminatorBeforeDeclaredOutputLength()
    {
        byte[] compressed =
        [
            0x02, 0x00, 0x00, 0x00, 0x00, 0xA9,
            0x02, 0x00,
            0x00, 0x00, 0x00,
        ];

        var error = Assert.Throws<InvalidDataException>(() => HsqCompression.Decompress(compressed));

        Assert.Contains("declared output length", error.Message, StringComparison.Ordinal);
    }
}
