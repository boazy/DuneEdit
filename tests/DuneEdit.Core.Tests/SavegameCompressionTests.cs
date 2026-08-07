using DuneEdit.Core;

namespace DuneEdit.Core.Tests;

public sealed class SavegameCompressionTests
{
    [Fact]
    public void DecompressesMarkerAtFirstByteAfterHeader()
    {
        byte[] compressed = [0x02, 0x00, 0xF7, 0x02, 0x07, 0x00, 0xF7, 0x04, 0x2A, 0x10];

        var result = SavegameCompression.Decompress(compressed);

        Assert.Equal<byte>([0x02, 0x00, 0xF7, 0x02, 0x07, 0x00, 0x2A, 0x2A, 0x2A, 0x2A, 0x10], result);
    }

    [Fact]
    public void CompressesRunsAndLiteralMarkerWithoutChangingPayload()
    {
        byte[] raw = [0x02, 0x00, 0xF7, 0x02, 0x00, 0x00, 0xF7, 0xF7, 0xF7, 0x01, 0x01, 0x02];

        var compressed = SavegameCompression.Compress(raw);
        var decompressed = SavegameCompression.Decompress(compressed);

        Assert.Equal(compressed.Length, SavegameCompression.ReadDeclaredFileLength(compressed));
        Assert.Equal(raw[..4], decompressed[..4]);
        Assert.Equal(raw[6..], decompressed[6..]);
    }

    [Fact]
    public void RejectsTruncatedMarkerSequence()
    {
        byte[] compressed = [0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0xF7];

        var error = Assert.Throws<InvalidDataException>(() => SavegameCompression.Decompress(compressed));

        Assert.Contains("ends inside", error.Message);
    }

    [Fact]
    public void RejectsInputWithoutHeader()
    {
        Assert.Throws<InvalidDataException>(() => SavegameCompression.Compress([0x01, 0x02]));
        Assert.Throws<InvalidDataException>(() => SavegameCompression.Decompress([0x01, 0x02]));
    }
}
