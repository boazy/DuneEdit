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

        Assert.Equal(LocationSignatures.CompressedSave.Length - 1, savegame.Locations.Count);
        Assert.Equal("Carthag (Atreides Palace)", savegame.Locations[0].Name);
        Assert.NotNull(savegame.FindLocation(new LocationId(0x0C, 0x06)));
    }

    [Fact]
    public void ParsesEveryLocationFromExecutable()
    {
        var executable = CreateDecompressedDocument(LocationSignatures.Executable);

        var savegame = DuneSavegame.Parse(executable, SavegameFormat.Executable);

        Assert.Equal(LocationSignatures.Executable.Length - 1, savegame.Locations.Count);
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

        location.RawTypeCode = 0x30;
        location.InventoryVisible = false;
        Assert.Equal(AreaController.Harkonnen, location.Controller);

        location.InventoryVisible = true;
        Assert.Equal(AreaController.Atreides, location.Controller);

        location.InventoryVisible = false;
        location.RawTypeCode = 0x21;
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
    public void EditingDesertAroundLocationCanBeParsedAgain()
    {
        var original = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);
        var editedLocation = original.Locations[12];
        editedLocation.DesertAround ^= 0xFF;

        var reparsed = DuneSavegame.Parse(original.ToFileBytes(), SavegameFormat.CompressedSave);

        Assert.Equal(editedLocation.DesertAround, reparsed.Locations[12].DesertAround);
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
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id.Value == 1);

        troop.RawJobCode = 28;
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
        troop.MilitaryRank = 99;
        troop.People = 340;
        troop.HasWeirdingModules = true;

        var reparsed = DuneSavegame.Parse(savegame.ToFileBytes(), SavegameFormat.CompressedSave);
        var persisted = Assert.Single(reparsed.FremenTroops, troop => troop.Id.Value == 1);
        Assert.Equal(31, persisted.RawJobCode);
        Assert.Equal(TroopOccupation.Spice, persisted.OccupationInfo.Occupation);
        Assert.Equal(TroopJob.SearchingForEquipment, persisted.OccupationInfo.Job);
        Assert.True(persisted.OccupationInfo.JobCompleted);
        Assert.Equal(TroopAllegiance.Harkonnen, persisted.OccupationInfo.Allegiance);
        Assert.Equal(100, persisted.Motivation);
        Assert.Equal(99, persisted.MilitaryRank);
        Assert.Equal(340, persisted.People);
        Assert.True(persisted.HasWeirdingModules);
    }

    [Fact]
    public void EveryRawJobCodeRoundTripsThroughOccupationInfo()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id.Value == 1);

        for (var rawJobCode = byte.MinValue; rawJobCode < byte.MaxValue; rawJobCode++)
        {
            var info = TroopOccupationInfo.FromRawJobCode(rawJobCode);
            Assert.Equal(rawJobCode, info.RawJobCode);
            troop.ApplyOccupationInfo(info);
            Assert.Equal(rawJobCode, troop.RawJobCode);
        }

        var finalInfo = TroopOccupationInfo.FromRawJobCode(byte.MaxValue);
        troop.ApplyOccupationInfo(finalInfo);
        Assert.Equal(byte.MaxValue, troop.RawJobCode);
    }

    [Fact]
    public void PreservesUnknownRawJobUntilAConcreteOccupationIsSelected()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id.Value == 2);

        troop.RawJobCode = 33;
        var rawState = troop.OccupationInfo;
        Assert.True(rawState.IsUnknown);
        Assert.Equal(33, rawState.RawJobCode);
        troop.ApplyOccupationInfo(rawState);
        Assert.Equal(33, troop.RawJobCode);

        var details = new FremenTroopDetailsViewModel(troop, savegame.Locations[0]);
        Assert.Contains(TroopOccupation.Unknown, details.AvailableOccupations);
        Assert.False(details.IsJobEnabled);
        Assert.False(details.IsJobCompletedEnabled);

        details.SelectedOccupation = TroopOccupation.Spice;
        Assert.Equal(0, troop.RawJobCode);
        Assert.Equal(TroopJob.Mining, troop.OccupationInfo.Job);
        Assert.Equal(TroopAllegiance.Atreides, troop.OccupationInfo.Allegiance);
    }

    [Fact]
    public void StructuredEditorEncodesJobCompletionAndRestrictsInapplicableControls()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id.Value == 1);

        troop.RawJobCode = 0;
        var details = new FremenTroopDetailsViewModel(troop, savegame.Locations[0]);
        Assert.True(details.IsJobEnabled);
        Assert.True(details.IsAllegianceEnabled);
        Assert.True(details.IsJobCompletedEnabled);

        details.JobCompleted = true;
        Assert.Equal(16, troop.RawJobCode);
        details.SelectedJob = TroopJob.SearchingForEquipment;
        Assert.Equal(19, troop.RawJobCode);
        details.SelectedAllegiance = TroopAllegiance.Harkonnen;
        Assert.Equal(28, troop.RawJobCode);
        details.SelectedJob = TroopJob.SearchingForEquipment;
        Assert.Equal(31, troop.RawJobCode);

        troop.RawJobCode = 129;
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
    public void LocationSequencesPreserveKnownBoundaries()
    {
        var compressedFirst = LocationSignatures.CompressedSave[0];
        var compressedLast = LocationSignatures.CompressedSave[^1];
        var executableFirst = LocationSignatures.Executable[0];
        var executableLast = LocationSignatures.Executable[^1];

        Assert.Equal((0x02, 0x01, 0x15), (compressedFirst.RegionId, compressedFirst.SubregionId, compressedFirst.Terminator));
        Assert.Equal((0xFF, 0xFF, 0x01), (compressedLast.RegionId, compressedLast.SubregionId, compressedLast.Terminator));
        Assert.Equal((0x02, 0x01, 0x15), (executableFirst.RegionId, executableFirst.SubregionId, executableFirst.Terminator));
        Assert.Equal((0xFF, 0xFF, 0x01), (executableLast.RegionId, executableLast.SubregionId, executableLast.Terminator));
        Assert.Equal(LocationSignatures.CompressedSave.Length, LocationSignatures.Executable.Length);
    }

    [Theory]
    [InlineData(0x00, LocationKind.Sietch, "Sietch")]
    [InlineData(0x10, LocationKind.Sietch, "Sietch")]
    [InlineData(0x20, LocationKind.CarthagPalace, "Carthag Palace")]
    [InlineData(0x21, LocationKind.Village, "Village")]
    [InlineData(0x22, LocationKind.Fort, "Fort")]
    [InlineData(0x2F, LocationKind.Fort, "Fort")]
    [InlineData(0x30, LocationKind.ArrakeenPalace, "Arrakeen Palace")]
    [InlineData(0x31, LocationKind.Unknown, "Unknown")]
    public void LocationTypeCodesPreserveKindAndTitle(
        byte rawType,
        LocationKind expectedKind,
        string expectedTitle)
    {
        var savegame = DuneSavegame.Parse(CreateCompressedDocument(), SavegameFormat.CompressedSave);
        var location = savegame.Locations[0];

        location.RawTypeCode = rawType;

        Assert.Equal(expectedKind, location.Kind);
        Assert.Equal(expectedTitle, new LocationDetailsViewModel(location).Type);
    }

    [Fact]
    public void TroopChainMapsEveryLinkedTroopToItsLocationAndTerminatesCycles()
    {
        var document = CreateDocumentWithFremenTroops();
        var firstTroopOffset = GetFremenTroopsOffset();
        document[firstTroopOffset + FremenTroop.RecordSize + 1] = 1;

        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(document),
            SavegameFormat.CompressedSave);

        var location = savegame.FindLocation(new LocationId(0x02, 0x01));
        Assert.Same(location, savegame.FindFremenTroopLocation(new TroopId(1)));
        Assert.Same(location, savegame.FindFremenTroopLocation(new TroopId(2)));
        Assert.Null(savegame.FindFremenTroopLocation(new TroopId(3)));
        Assert.Equal(2, savegame.FremenTroops.Count);
    }

    [Fact]
    public void MilitaryRankEditorPreservesRankAndLabel()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var troop = Assert.Single(savegame.FremenTroops, troop => troop.Id.Value == 1);
        var details = new FremenTroopDetailsViewModel(troop, savegame.Locations[0]);
        var militaryRank = Assert.Single(details.Attributes, attribute => attribute.Label == "Military rank");

        militaryRank.Value = 77;

        Assert.Equal(77, troop.MilitaryRank);
        Assert.Equal(77, militaryRank.Value);
    }

    [Fact]
    public void SelectionTransitionsKeepExactlyOneActiveTarget()
    {
        var savegame = DuneSavegame.Parse(
            SavegameCompression.Compress(CreateDocumentWithFremenTroops()),
            SavegameFormat.CompressedSave);
        var editor = new MainViewModel(new NoopPlatformService());
        var location = savegame.Locations[0];
        var troop = savegame.FremenTroops[0];
        var locationMarker = CreateUninitialized<LocationMarkerViewModel>();
        var troopMarker = CreateUninitialized<FremenTroopMarkerViewModel>();
        SetAutoProperty(locationMarker, nameof(LocationMarkerViewModel.Location), location);
        SetAutoProperty(troopMarker, nameof(FremenTroopMarkerViewModel.Location), location);
        SetAutoProperty(troopMarker, nameof(FremenTroopMarkerViewModel.Troop), troop);

        InvokeSelection(editor, "SelectLocation", locationMarker);
        Assert.True(locationMarker.IsSelected);
        Assert.NotNull(editor.SelectedLocation);
        Assert.Null(editor.SelectedFremenTroop);
        Assert.True(editor.HasLocationSelection);
        Assert.False(editor.HasFremenTroopSelection);

        InvokeSelection(editor, "SelectFremenTroop", troopMarker);
        Assert.False(locationMarker.IsSelected);
        Assert.True(troopMarker.IsSelected);
        Assert.Null(editor.SelectedLocation);
        Assert.NotNull(editor.SelectedFremenTroop);
        Assert.False(editor.HasLocationSelection);
        Assert.True(editor.HasFremenTroopSelection);

        InvokeSelection(editor, "SelectFremenTroop", troopMarker);
        Assert.False(troopMarker.IsSelected);
        Assert.Null(editor.SelectedLocation);
        Assert.Null(editor.SelectedFremenTroop);
        Assert.False(editor.HasSelection);
    }

    [Fact]
    public void MarkerProjectionPreservesEncodedCoordinatesAndTroopPlacement()
    {
        var locationCenter = MapProjection.Project(
            new MapPosition(3, 4),
            width: 1000,
            height: 620,
            margin: 20);
        var placementOffset = MapProjection.GetTroopOffset(TroopPlacement.South);

        Assert.Equal(31, locationCenter.X);
        Assert.Equal(325, locationCenter.Y);
        Assert.Equal(0, placementOffset.X);
        Assert.Equal(13, placementOffset.Y);
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
        return SavegameCompression.Compress(CreateDecompressedDocument(LocationSignatures.CompressedSave));
    }

    private static byte[] CreateDecompressedDocument(ReadOnlySpan<LocationSignature> sequences)
    {
        var length = LocationBlockOffset + ((sequences.Length - 1) * DuneLocation.RecordSize) + 3;
        var data = new byte[length];
        data[0] = 0x02;
        data[2] = SavegameCompression.Marker;
        data[3] = 0x02;

        for (var index = SavegameCompression.HeaderLength; index < LocationBlockOffset; index++)
        {
            data[index] = (byte)(0x80 + index);
        }

        for (var index = 0; index < sequences.Length; index++)
        {
            var sequence = sequences[index];
            var offset = LocationBlockOffset + (index * DuneLocation.RecordSize);
            data[offset] = sequence.RegionId;
            data[offset + 1] = sequence.SubregionId;
            data[offset + 2] = sequence.Terminator;

            if (index < sequences.Length - 1)
            {
                for (var field = 3; field < DuneLocation.RecordSize; field++)
                {
                    data[offset + field] = (byte)((index + field) & 0xFF);
                }
            }
        }

        return data;
    }

    private static byte[] CreateDocumentWithFremenTroops()
    {
        var data = CreateDecompressedDocument(LocationSignatures.CompressedSave);
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

    private static int GetFremenTroopsOffset() =>
        LocationBlockOffset + ((LocationSignatures.CompressedSave.Length - 1) * DuneLocation.RecordSize) + 2;

    private static T CreateUninitialized<T>() where T : class =>
        (T)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static void SetAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"No backing field exists for {propertyName}.");
        field.SetValue(target, value);
    }

    private static void InvokeSelection(MainViewModel editor, string methodName, object marker)
    {
        var method = typeof(MainViewModel).GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"No selection method named {methodName} exists.");
        method.Invoke(editor, [marker]);
    }


    private sealed class NoopPlatformService : IPlatformService
    {
        public Task<string?> OpenDuneFileAsync() => Task.FromResult<string?>(null);

        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
