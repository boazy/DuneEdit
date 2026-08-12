using Avalonia;
using Avalonia.Headless;
using DuneEdit.Desktop;
using DuneEdit.Desktop.Services;
using DuneEdit.Desktop.ViewModels;

namespace DuneEdit.Core.Tests;

public static class HeadlessTestApplication
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class DesktopSmokeTests
{
    [Fact]
    public async Task ArtifactCompatibilityScenarioRunsEndToEnd()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApplication));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await session.Dispatch(
            async () =>
            {
                var viewModel = new MainViewModel(new NoopPlatformService());
                await ArtifactCompatibilitySmoke.RunAsync(viewModel);
                Assert.Equal(LocationSignatures.CompressedSave.Length - 1, viewModel.LocationMarkers.Count);
            },
            timeout.Token);
    }

    private sealed class NoopPlatformService : IPlatformService
    {
        public Task<string?> OpenDuneFileAsync() => Task.FromResult<string?>(null);

        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
