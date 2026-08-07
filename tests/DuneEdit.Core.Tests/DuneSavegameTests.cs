using DuneEdit.Core;

namespace DuneEdit.Core.Tests;

public sealed class DuneSavegameTests
{
    private const int LocationBlockOffset = SavegameCompression.HeaderLength + 13;

    [Fact]
    public void ParsesEveryLocationFromCompressedSave()
    {
        var savegame = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);

        Assert.Equal(LocSequences.compressed.Length - 1, savegame.Locations.Count);
        Assert.Equal("Carthag (Atreides Palace)", savegame.Locations[0].Name);
        Assert.NotNull(savegame.FindLocation(0x0C, 0x06));
    }

    [Fact]
    public void ParsesEveryLocationFromExecutable()
    {
        var executable = CreateDecompressedDocument(LocSequences.uncompressed);

        var savegame = DuneSavegame.Parse(executable, SavegameFormat.Executable);

        Assert.Equal(LocSequences.uncompressed.Length - 1, savegame.Locations.Count);
        Assert.Equal(SavegameFormat.Executable, savegame.Format);
    }

    [Fact]
    public void EditingFieldChangesOnlyMappedByte()
    {
        var savegame = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);
        var before = savegame.ToDecompressedBytes();
        var firstLocation = savegame.Locations[0];

        firstLocation.Water ^= 0x5A;
        var after = savegame.ToDecompressedBytes();

        var changedOffsets = before
            .Select((value, offset) => (value, offset))
            .Where(item => item.value != after[item.offset])
            .Select(item => item.offset)
            .ToArray();
        Assert.Equal([LocationBlockOffset + 0x1B], changedOffsets);
    }

    [Fact]
    public void StatusPropertiesMapToExpectedBits()
    {
        var savegame = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);
        var location = savegame.Locations[0];
        var initial = location.ToArray()[0x0A];

        location.Vegetation = true;
        location.Prospected = true;
        location.Discovered = false;

        Assert.Equal((byte)(initial | 0b1100_0001), location.ToArray()[0x0A]);
        Assert.True(location.Vegetation);
        Assert.True(location.Prospected);
        Assert.False(location.Discovered);
    }

    [Fact]
    public void SerializedSaveCanBeParsedAgain()
    {
        var original = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);
        original.Locations[4].Spice = 0xA5;

        var reparsed = DuneSavegame.Parse(original.ToFileBytes(), SavegameFormat.CompressedSave);

        Assert.Equal(0xA5, reparsed.Locations[4].Spice);
        Assert.Equal(original.Locations.Select(location => location.ToArray()), reparsed.Locations.Select(location => location.ToArray()));
    }

    [Fact]
    public void EditingDesertAroundSietchCanBeParsedAgain()
    {
        var original = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);
        var editedLocation = original.Locations[12];
        editedLocation.DesertAroundSietch ^= 0xFF;

        var reparsed = DuneSavegame.Parse(original.ToFileBytes(), SavegameFormat.CompressedSave);

        Assert.Equal(editedLocation.DesertAroundSietch, reparsed.Locations[12].DesertAroundSietch);
        Assert.Equal(
            original.Locations.Select(location => location.ToArray()),
            reparsed.Locations.Select(location => location.ToArray()));
    }

    [Fact]
    public void RejectsFileWithoutCompleteLocationBlock()
    {
        byte[] invalid = [0, 0, 0, 0, 4, 0];

        var error = Assert.Throws<InvalidDataException>(() => DuneSavegame.Parse(invalid, SavegameFormat.CompressedSave));

        Assert.Contains("location block", error.Message);
    }

    private static byte[] CreateCompressedDocument()
    {
        return SavegameCompression.Compress(CreateDecompressedDocument(LocSequences.compressed));
    }

    private static byte[] CreateDecompressedDocument(IReadOnlyList<Loc> sequences)
    {
        var length = LocationBlockOffset + ((sequences.Count - 1) * Sietch.RecordSize) + 3;
        var data = new byte[length];
        data[0] = 0x02;
        data[2] = SavegameCompression.Marker;
        data[3] = 0x02;

        for (var index = SavegameCompression.HeaderLength; index < LocationBlockOffset; index++)
        {
            data[index] = (byte)(0x80 + index);
        }

        for (var index = 0; index < sequences.Count; index++)
        {
            var sequence = sequences[index];
            var offset = LocationBlockOffset + (index * Sietch.RecordSize);
            data[offset] = sequence.v1;
            data[offset + 1] = sequence.v2;
            data[offset + 2] = sequence.v3;

            if (index < sequences.Count - 1)
            {
                for (var field = 3; field < Sietch.RecordSize; field++)
                {
                    data[offset + field] = (byte)((index + field) & 0xFF);
                }
            }
        }

        return data;
    }
}
