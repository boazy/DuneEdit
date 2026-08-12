using DuneEdit.Core;
using DuneEdit.Desktop.ViewModels;

namespace DuneEdit.Desktop;

internal static class ArtifactCompatibilitySmoke
{
    private const int LocationBlockOffset = SavegameCompression.HeaderLength + 13;

    public static async Task RunAsync(MainViewModel viewModel)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"duneedit-smoke-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "DUNE21S0.SAV");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllBytes(filePath, CreateCompressedFixture());
            await viewModel.LoadFileAsync(filePath);

            if (viewModel.LocationMarkers.Count != LocationSignatures.CompressedSave.Length - 1)
            {
                throw new InvalidOperationException("The desktop editor did not load every fixture location.");
            }

            if (viewModel.SelectedMapFilter != MapFilter.None)
            {
                throw new InvalidOperationException("The editor did not default to the unfiltered map.");
            }

            viewModel.SelectAreaControlFilterCommand.Execute(null);
            var discoveredDesertMarker = viewModel.LocationMarkers.FirstOrDefault(marker =>
                marker.Location.Discovered && marker.Location.Controller == AreaController.Desert)
                ?? throw new InvalidOperationException("The fixture did not contain a discovered desert location.");
            if (!discoveredDesertMarker.IsVisible)
            {
                throw new InvalidOperationException("The control map hid a desert location marker.");
            }

            discoveredDesertMarker.SelectCommand.Execute(null);
            if (viewModel.SelectedLocation?.IsDesertControlled != true)
            {
                throw new InvalidOperationException("The editor did not select the desert location.");
            }

            var undiscoveredControlledMarker = viewModel.LocationMarkers.FirstOrDefault(marker =>
                marker.Location.Controller != AreaController.Desert)
                ?? throw new InvalidOperationException("The fixture did not contain a controlled location.");
            undiscoveredControlledMarker.Location.Discovered = false;
            viewModel.SelectNoMapFilterCommand.Execute(null);
            viewModel.SelectAreaControlFilterCommand.Execute(null);
            if (!undiscoveredControlledMarker.IsVisible)
            {
                throw new InvalidOperationException("The control map hid an undiscovered controlled location.");
            }

            var location = viewModel.LocationMarkers[12].Location;
            var details = new LocationDetailsViewModel(location);
            var desertField = details.Advanced.Single(field => field.Label == "Desert around");
            var editedValue = (byte)(location.DesertAround ^ 0x5A);
            desertField.Value = editedValue;

            if (location.DesertAround != editedValue)
            {
                throw new InvalidOperationException("The desktop editor did not apply the fixture edit.");
            }

            await viewModel.SaveFileAsync();
            var reparsed = DuneSavegame.Load(filePath);
            if (reparsed.Locations[12].DesertAround != editedValue)
            {
                throw new InvalidOperationException("The saved fixture did not retain the edited value.");
            }

            await viewModel.LoadFileAsync(filePath);
            if (viewModel.LocationMarkers[12].Location.DesertAround != editedValue)
            {
                throw new InvalidOperationException("The desktop editor did not retain the edit after reopening the fixture.");
            }

            Console.WriteLine("DUNEEDIT_ARTIFACT_COMPATIBILITY_OK");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static byte[] CreateCompressedFixture()
    {
        var sequences = LocationSignatures.CompressedSave;
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

            if (index >= sequences.Length - 1)
            {
                continue;
            }

            for (var field = 3; field < DuneLocation.RecordSize; field++)
            {
                data[offset + field] = (byte)((index + field) & 0xFF);
            }
        }

        return SavegameCompression.Compress(data);
    }
}
