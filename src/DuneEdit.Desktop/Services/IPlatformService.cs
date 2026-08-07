namespace DuneEdit.Desktop.Services;

public interface IPlatformService
{
    Task<string?> OpenDuneFileAsync();
    Task ShowErrorAsync(string title, string message);
}
