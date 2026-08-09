using DuneEdit.Desktop.ViewModels;
using DuneEdit.Desktop.Services;
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
    public void SpiceFieldsMapToDistinctBytes()
    {
        var savegame = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);
        var location = savegame.Locations[0];

        location.Spice = 0x42;
        location.SpiceDensity = 0x99;

        Assert.Equal(0x42, location.ToArray()[0x11]);
        Assert.Equal(0x99, location.ToArray()[0x12]);
    }

    [Fact]
    public void AreaControllerFollowsGameOwnershipFlags()
    {
        var savegame = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);
        var location = savegame.Locations[0];

        location.LocationType = 0x30;
        location.InventoryVisible = false;
        Assert.Equal(AreaController.Harkonnen, location.Controller);

        location.InventoryVisible = true;
        Assert.Equal(AreaController.Atreides, location.Controller);

        location.InventoryVisible = false;
        location.LocationType = 0x21;
        Assert.Equal(AreaController.Desert, location.Controller);
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
    public void ParsesAndPersistsStructuredFremenTroopOccupation()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id == 1);

        troop.Occupation = 28;
        Assert.Equal(TroopOccupation.Spice, troop.OccupationInfo.Occupation);
        Assert.Equal(TroopJob.Mining, troop.OccupationInfo.Job);
        Assert.True(troop.OccupationInfo.JobCompleted);
        Assert.Equal(TroopAllegiance.Harkonnen, troop.OccupationInfo.Allegiance);

        troop.ApplyOccupationInfo(TroopOccupationInfo.CreateEdited(
            TroopOccupation.Spice,
            TroopJob.SearchingForEquipment,
            true,
            TroopAllegiance.Harkonnen));
        troop.Motivation = 100;
        troop.ArmyRank = 99;
        troop.People = 340;
        troop.HasWeirdingModules = true;

        var reparsed = DuneSavegame.Parse(savegame.ToFileBytes(), SavegameFormat.CompressedSave);
        var persisted = Assert.Single(reparsed.FremenTroops, troop => troop.Id == 1);
        Assert.Equal(31, persisted.Occupation);
        Assert.Equal(TroopOccupation.Spice, persisted.OccupationInfo.Occupation);
        Assert.Equal(TroopJob.SearchingForEquipment, persisted.OccupationInfo.Job);
        Assert.True(persisted.OccupationInfo.JobCompleted);
        Assert.Equal(TroopAllegiance.Harkonnen, persisted.OccupationInfo.Allegiance);
        Assert.Equal(100, persisted.Motivation);
        Assert.Equal(99, persisted.ArmyRank);
        Assert.Equal(340, persisted.People);
        Assert.True(persisted.HasWeirdingModules);
    }

    [Fact]
    public void EveryRawJobCodeRoundTripsThroughOccupationInfo()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id == 1);

        for (var rawJobCode = byte.MinValue; rawJobCode < byte.MaxValue; rawJobCode++)
        {
            var info = TroopOccupationInfo.FromRawJobCode(rawJobCode);
            Assert.Equal(rawJobCode, info.RawJobCode);
            troop.ApplyOccupationInfo(info);
            Assert.Equal(rawJobCode, troop.Occupation);
        }

        var finalInfo = TroopOccupationInfo.FromRawJobCode(byte.MaxValue);
        troop.ApplyOccupationInfo(finalInfo);
        Assert.Equal(byte.MaxValue, troop.Occupation);
    }

    [Fact]
    public void PreservesUnknownRawJobUntilAConcreteOccupationIsSelected()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id == 2);

        troop.Occupation = 33;
        var rawState = troop.OccupationInfo;
        Assert.True(rawState.IsUnknown);
        Assert.Equal(33, rawState.RawJobCode);
        troop.ApplyOccupationInfo(rawState);
        Assert.Equal(33, troop.Occupation);

        var details = new FremenTroopDetailsViewModel(troop, savegame.Locations[0]);
        Assert.Contains(TroopOccupation.Unknown, details.AvailableOccupations);
        Assert.False(details.IsJobEnabled);
        Assert.False(details.IsJobCompletedEnabled);

        details.SelectedOccupation = TroopOccupation.Spice;
        Assert.Equal(0, troop.Occupation);
        Assert.Equal(TroopJob.Mining, troop.OccupationInfo.Job);
        Assert.Equal(TroopAllegiance.Atreides, troop.OccupationInfo.Allegiance);
    }

    [Fact]
    public void StructuredEditorEncodesJobCompletionAndRestrictsInapplicableControls()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id == 1);

        troop.Occupation = 0;
        var details = new FremenTroopDetailsViewModel(troop, savegame.Locations[0]);
        Assert.True(details.IsJobEnabled);
        Assert.True(details.IsAllegianceEnabled);
        Assert.True(details.IsJobCompletedEnabled);

        details.JobCompleted = true;
        Assert.Equal(16, troop.Occupation);
        details.SelectedJob = TroopJob.SearchingForEquipment;
        Assert.Equal(19, troop.Occupation);
        details.SelectedAllegiance = TroopAllegiance.Harkonnen;
        Assert.Equal(28, troop.Occupation);
        details.SelectedJob = TroopJob.SearchingForEquipment;
        Assert.Equal(31, troop.Occupation);

        troop.Occupation = 129;
        var unrecruitedDetails = new FremenTroopDetailsViewModel(troop, savegame.Locations[0]);
        Assert.Equal(TroopOccupation.Unrecruited, unrecruitedDetails.SelectedOccupation);
        Assert.False(unrecruitedDetails.IsJobEnabled);
        Assert.False(unrecruitedDetails.IsAllegianceEnabled);
        Assert.False(unrecruitedDetails.IsJobCompletedEnabled);
    }

    [Fact]
    public void TroopVisibilityCommandUpdatesStateAndTooltip()
    {
        var editor = new MainViewModel(new NoopPlatformService());

        editor.ToggleTroopDisplayCommand.Execute(null);

        Assert.False(editor.IsTroopDisplayEnabled);
        Assert.Equal("Fremen troops hidden. Click to show them.", editor.TroopDisplayToolTip);
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

    private static byte[] CreateDocumentWithFremenTroops()
    {
        var data = CreateDecompressedDocument(LocSequences.compressed);
        var firstTroopOffset = data.Length - 1;
        Array.Resize(ref data, firstTroopOffset + (FremenTroop.RecordSize * 68));
        data[LocationBlockOffset + 0x09] = 1;

        data[firstTroopOffset] = 1;
        data[firstTroopOffset + 1] = 2;
        data[firstTroopOffset + 2] = 1;
        data[firstTroopOffset + 3] = 128;
        data[firstTroopOffset + 0x15] = 80;
        data[firstTroopOffset + 0x16] = 20;
        data[firstTroopOffset + 0x17] = 30;
        data[firstTroopOffset + 0x18] = 40;
        data[firstTroopOffset + 0x19] = 1 << 4;
        data[firstTroopOffset + 0x1A] = 25;

        var secondTroopOffset = firstTroopOffset + FremenTroop.RecordSize;
        data[secondTroopOffset] = 2;
        data[secondTroopOffset + 3] = 129;
        return data;
    }

    private sealed class NoopPlatformService : IPlatformService
    {
        public Task<string?> OpenDuneFileAsync() => Task.FromResult<string?>(null);

        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
