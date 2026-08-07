using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DuneEdit.Desktop.Services;
using DuneEdit.Desktop.ViewModels;
using DuneEdit.Desktop.Views;

namespace DuneEdit.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var viewModel = new MainViewModel(new AvaloniaPlatformService(window));
            window.DataContext = viewModel;
            desktop.MainWindow = window;

            if (desktop.Args?.Contains("--smoke-test", StringComparer.Ordinal) == true)
            {
                window.Opened += (_, _) =>
                {
                    Console.WriteLine("DUNEEDIT_DESKTOP_SMOKE_OK");
                    Dispatcher.UIThread.Post(() => desktop.Shutdown(0), DispatcherPriority.Background);
                };
            }
            else if (desktop.Args is [var filePath, ..] && File.Exists(filePath))
            {
                window.Opened += async (_, _) => await viewModel.LoadFileAsync(filePath);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}