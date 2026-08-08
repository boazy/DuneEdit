using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DuneEdit.Core;
using DuneEdit.Desktop.Services;

namespace DuneEdit.Desktop.ViewModels;

public partial class MainViewModel(IPlatformService platform) : ViewModelBase
{
    private DuneSavegame? document;
    private LocationMarkerViewModel? selectedMarker;

    [ObservableProperty]
    public partial string CurrentFileName { get; private set; } = "No file open";

    [ObservableProperty]
    public partial string StatusText { get; private set; } = "Open a Dune save to begin.";

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial LocationDetailsViewModel? SelectedLocation { get; private set; }


    [ObservableProperty]
    public partial WriteableBitmap? MapFilterImage { get; private set; }

    [ObservableProperty]
    public partial MapFilter SelectedMapFilter { get; set; } = MapFilter.AreaControl;
    public ObservableCollection<LocationMarkerViewModel> Locations { get; } = [];
    public bool HasDocument => document is not null;
    public bool HasNoDocument => document is null;
    public bool HasSelection => SelectedLocation is not null;
    public bool HasNoSelection => SelectedLocation is null;
    public bool IsAreaControlFilter => SelectedMapFilter == MapFilter.AreaControl;
    public bool IsSpiceDensityFilter => SelectedMapFilter == MapFilter.SpiceDensity;
    public bool IsDiscoveryFilter => SelectedMapFilter == MapFilter.Discovery;

    public bool IsNoMapFilter => SelectedMapFilter == MapFilter.None;

    [RelayCommand]
    private void SelectNoMapFilter() => SelectedMapFilter = MapFilter.None;

    [RelayCommand]
    private void SelectAreaControlFilter() => SelectedMapFilter = MapFilter.AreaControl;

    [RelayCommand]
    private void SelectSpiceDensityFilter() => SelectedMapFilter = MapFilter.SpiceDensity;

    [RelayCommand]
    private void SelectDiscoveryFilter() => SelectedMapFilter = MapFilter.Discovery;


    [RelayCommand(CanExecute = nameof(CanOpen))]
    private async Task OpenAsync()
    {
        var filePath = await platform.OpenDuneFileAsync();
        if (filePath is not null)
        {
            await LoadFileAsync(filePath);
        }
    }

    public async Task LoadFileAsync(string filePath)
    {
        IsBusy = true;
        try
        {
            var loaded = await Task.Run(() => DuneSavegame.Load(filePath));
            document = loaded;
            CurrentFileName = Path.GetFileName(filePath);

            Locations.Clear();
            foreach (var location in loaded.Locations)
            {
                Locations.Add(new LocationMarkerViewModel(location, SelectLocation));
            }

            SelectLocation(null);
            RefreshMapVisuals();
            StatusText = $"Loaded {loaded.Locations.Count} locations from {CurrentFileName}.";
            OnPropertyChanged(nameof(HasDocument));
            OnPropertyChanged(nameof(HasNoDocument));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            await platform.ShowErrorAsync("Unable to open Dune file", error.Message);
            StatusText = "The selected file could not be opened.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync() => SaveFileAsync();

    public async Task SaveFileAsync()
    {
        if (document is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Run(() => document.Save());
            StatusText = $"Saved {CurrentFileName}.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await platform.ShowErrorAsync("Unable to save Dune file", error.Message);
            StatusText = "Changes were not saved.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanOpen() => !IsBusy;
    private bool CanSave() => document is not null && !IsBusy;

    private void SelectLocation(LocationMarkerViewModel? marker)
    {
        if (marker is not null && ReferenceEquals(selectedMarker, marker))
        {
            marker = null;
        }

        if (selectedMarker is not null)
        {
            selectedMarker.IsSelected = false;
        }

        selectedMarker = marker;
        if (selectedMarker is not null)
        {
            selectedMarker.IsSelected = true;
        }

        SelectedLocation = marker is null ? null : new LocationDetailsViewModel(marker.Location, RefreshMapVisuals);
        StatusText = marker is null
            ? document is null ? "Open a Dune save to begin." : "Select a location on the map."
            : $"Editing {marker.Name}.";
    }

    [RelayCommand]
    private void CloseSelection() => SelectLocation(null);

    private void RefreshMapVisuals()
    {
        if (document is null)
        {
            return;
        }

        foreach (var marker in Locations)
        {
            marker.IsVisible = SelectedMapFilter != MapFilter.Discovery || marker.Location.Discovered;
        }

        if (SelectedMapFilter == MapFilter.Discovery && selectedMarker?.Location.Discovered == false)
        {
            SelectLocation(null);
        }

        var previousImage = MapFilterImage;
        MapFilterImage = SelectedMapFilter == MapFilter.None
            ? null
            : MapFilterImageRenderer.Render(document.Locations, SelectedMapFilter);
        previousImage?.Dispose();
    }

    partial void OnSelectedMapFilterChanged(MapFilter value)
    {
        OnPropertyChanged(nameof(IsAreaControlFilter));
        OnPropertyChanged(nameof(IsNoMapFilter));
        OnPropertyChanged(nameof(IsSpiceDensityFilter));
        OnPropertyChanged(nameof(IsDiscoveryFilter));
        RefreshMapVisuals();
    }

    partial void OnSelectedLocationChanged(LocationDetailsViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasNoSelection));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OpenCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }
}
