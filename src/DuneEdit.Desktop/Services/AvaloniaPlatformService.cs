using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DuneEdit.Desktop.Views;

namespace DuneEdit.Desktop.Services;

public sealed class AvaloniaPlatformService(Window owner) : IPlatformService
{
    private static readonly FilePickerFileType DuneFiles = new("Dune save or executable")
    {
        Patterns = ["*.sav", "*.SAV", "*.exe", "*.EXE"],
    };

    public async Task<string?> OpenDuneFileAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Dune file",
            AllowMultiple = false,
            FileTypeFilter = [DuneFiles],
        });

        return files.Count == 1 ? files[0].TryGetLocalPath() : null;
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new MessageDialog(title, message);
        await dialog.ShowDialog(owner);
    }
}
