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
    private FremenTroopMarkerViewModel? selectedFremenTroopMarker;

    [ObservableProperty]
    public partial string CurrentFileName { get; private set; } = "No file open";

    [ObservableProperty]
    public partial string StatusText { get; private set; } = "Open a Dune save to begin.";

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial LocationDetailsViewModel? SelectedLocation { get; private set; }

    [ObservableProperty]
    public partial FremenTroopDetailsViewModel? SelectedFremenTroop { get; private set; }


    [ObservableProperty]
    public partial WriteableBitmap? MapFilterImage { get; private set; }

    [ObservableProperty]
    public partial MapFilter SelectedMapFilter { get; set; } = MapFilter.None;

    [ObservableProperty]
    private TerrainDisplayMode terrainMode = TerrainDisplayMode.Enabled;

    [ObservableProperty]
    private bool isTroopDisplayEnabled = true;


    public ObservableCollection<LocationMarkerViewModel> Locations { get; } = [];
    public ObservableCollection<FremenTroopMarkerViewModel> FremenTroops { get; } = [];
    public bool HasDocument => document is not null;
    public bool HasNoDocument => document is null;
    public bool HasSelection => SelectedLocation is not null || SelectedFremenTroop is not null;
    public bool HasNoSelection => !HasSelection;
    public bool HasLocationSelection => SelectedLocation is not null;
    public bool HasFremenTroopSelection => SelectedFremenTroop is not null;
    public string? SelectedName => SelectedFremenTroop?.Name ?? SelectedLocation?.Name;
    public string? SelectedType => SelectedFremenTroop?.Type ?? SelectedLocation?.Type;
    public bool IsSelectionAtreides => GetSelectedController() == AreaController.Atreides;
    public bool IsSelectionDesert => GetSelectedController() == AreaController.Desert;
    public bool IsAreaControlFilter => SelectedMapFilter == MapFilter.AreaControl;
    public bool IsSpiceDensityFilter => SelectedMapFilter == MapFilter.SpiceDensity;
    public bool IsDiscoveryFilter => SelectedMapFilter == MapFilter.Discovery;
    public bool IsNoMapFilter => SelectedMapFilter == MapFilter.None;
    public bool IsTerrainVisible => TerrainMode != TerrainDisplayMode.Disabled;
    public bool IsTerrainEnabled => TerrainMode == TerrainDisplayMode.Enabled;
    public bool IsTerrainVisibleThroughFilter => TerrainMode == TerrainDisplayMode.VisibleThroughFilter;
    public bool IsTerrainDisabled => TerrainMode == TerrainDisplayMode.Disabled;
    public double MapFilterOpacity => TerrainMode == TerrainDisplayMode.VisibleThroughFilter
        && SelectedMapFilter != MapFilter.None
        ? 0.75
        : 1;
    public string TerrainToggleToolTip => TerrainMode switch
    {
        TerrainDisplayMode.Enabled => "Terrain shown. Click to show it through map filters.",
        TerrainDisplayMode.VisibleThroughFilter => "Terrain shows through map filters. Click to hide it.",
        _ => "Terrain hidden. Click to show the original game terrain.",
    };
    public string TroopDisplayToolTip => IsTroopDisplayEnabled
        ? "Fremen troops shown. Click to hide them."
        : "Fremen troops hidden. Click to show them.";


    [RelayCommand]
    private void CycleTerrainMode() => TerrainMode = TerrainMode switch
    {
        TerrainDisplayMode.Enabled => TerrainDisplayMode.VisibleThroughFilter,
        TerrainDisplayMode.VisibleThroughFilter => TerrainDisplayMode.Disabled,
        _ => TerrainDisplayMode.Enabled,
    };

    [RelayCommand]
    private void ToggleTroopDisplay() => IsTroopDisplayEnabled = !IsTroopDisplayEnabled;

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

            FremenTroops.Clear();
            foreach (var troop in loaded.FremenTroops)
            {
                var location = loaded.FindFremenTroopLocation(troop.Id);
                if (location is not null)
                {
                    FremenTroops.Add(new FremenTroopMarkerViewModel(troop, location, SelectFremenTroop));
                }
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

        if (selectedFremenTroopMarker is not null)
        {
            selectedFremenTroopMarker.IsSelected = false;
            selectedFremenTroopMarker = null;
            SelectedFremenTroop = null;
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
            ? document is null ? "Open a Dune save to begin." : "Select a location or Fremen troop on the map."
            : $"Editing {marker.Name}.";
    }

    private void SelectFremenTroop(FremenTroopMarkerViewModel? marker)
    {
        if (marker is not null && ReferenceEquals(selectedFremenTroopMarker, marker))
        {
            marker = null;
        }

        if (selectedMarker is not null)
        {
            selectedMarker.IsSelected = false;
            selectedMarker = null;
            SelectedLocation = null;
        }

        if (selectedFremenTroopMarker is not null)
        {
            selectedFremenTroopMarker.IsSelected = false;
        }

        selectedFremenTroopMarker = marker;
        if (selectedFremenTroopMarker is not null)
        {
            selectedFremenTroopMarker.IsSelected = true;
        }

        SelectedFremenTroop = marker is null
            ? null
            : new FremenTroopDetailsViewModel(marker.Troop, marker.Location, marker.RefreshDisplay);
        StatusText = marker is null
            ? document is null ? "Open a Dune save to begin." : "Select a location or Fremen troop on the map."
            : $"Editing Fremen troop {marker.Troop.Id:D2} at {marker.Location.Name}.";
    }

    [RelayCommand]
    private void CloseSelection()
    {
        if (selectedFremenTroopMarker is not null)
        {
            SelectFremenTroop(null);
        }
        else
        {
            SelectLocation(null);
        }
    }

    private void RefreshMapVisuals()
    {
        if (document is null)
        {
            return;
        }

        foreach (var marker in Locations)
        {
            marker.IsVisible = SelectedMapFilter switch
            {
                MapFilter.Discovery => marker.Location.Discovered,
                _ => true,
            };
        }

        foreach (var troop in FremenTroops)
        {
            troop.IsVisible = IsTroopDisplayEnabled && (SelectedMapFilter switch
            {
                MapFilter.Discovery => troop.Location.Discovered,
                _ => true,
            });
        }

        if (selectedMarker is not null && !selectedMarker.IsVisible)
        {
            SelectLocation(null);
        }

        if (selectedFremenTroopMarker is not null && !selectedFremenTroopMarker.IsVisible)
        {
            SelectFremenTroop(null);
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


        OnPropertyChanged(nameof(MapFilterOpacity));
    }


    partial void OnTerrainModeChanged(TerrainDisplayMode value)
    {
        OnPropertyChanged(nameof(IsTerrainEnabled));
        OnPropertyChanged(nameof(IsTerrainDisabled));
        OnPropertyChanged(nameof(IsTerrainVisible));
        OnPropertyChanged(nameof(IsTerrainVisibleThroughFilter));
        OnPropertyChanged(nameof(MapFilterOpacity));
        OnPropertyChanged(nameof(TerrainToggleToolTip));
    }

    partial void OnIsTroopDisplayEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(TroopDisplayToolTip));
        RefreshMapVisuals();
    }
    partial void OnSelectedLocationChanged(LocationDetailsViewModel? value)
    {
        NotifySelectionChanged();
    }

    partial void OnSelectedFremenTroopChanged(FremenTroopDetailsViewModel? value)
    {
        NotifySelectionChanged();
    }

    private AreaController? GetSelectedController() =>
        SelectedFremenTroop?.Location.Controller ?? SelectedLocation?.Location.Controller;

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasNoSelection));
        OnPropertyChanged(nameof(HasLocationSelection));
        OnPropertyChanged(nameof(HasFremenTroopSelection));
        OnPropertyChanged(nameof(SelectedName));
        OnPropertyChanged(nameof(SelectedType));
        OnPropertyChanged(nameof(IsSelectionAtreides));
        OnPropertyChanged(nameof(IsSelectionDesert));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OpenCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }
}
