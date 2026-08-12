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
    private IEditorSelection? activeSelection;

    [ObservableProperty]
    public partial string CurrentFileName { get; private set; } = "No file open";

    [ObservableProperty]
    public partial string StatusText { get; private set; } = "Open a Dune save to begin.";

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial WriteableBitmap? MapFilterImage { get; private set; }

    [ObservableProperty]
    public partial MapFilter SelectedMapFilter { get; set; } = MapFilter.None;

    [ObservableProperty]
    public partial TerrainDisplayMode TerrainMode { get; set; } = TerrainDisplayMode.Enabled;

    [ObservableProperty]
    public partial bool IsTroopDisplayEnabled { get; set; } = true;

    public ObservableCollection<LocationMarkerViewModel> LocationMarkers { get; } = [];
    public ObservableCollection<FremenTroopMarkerViewModel> FremenTroopMarkers { get; } = [];
    public LocationDetailsViewModel? SelectedLocation =>
        (activeSelection as LocationSelection)?.Details;
    public FremenTroopDetailsViewModel? SelectedFremenTroop =>
        (activeSelection as FremenTroopSelection)?.Details;
    public bool HasDocument => document is not null;
    public bool HasNoDocument => document is null;
    public bool HasSelection => activeSelection is not null;
    public bool HasNoSelection => activeSelection is null;
    public bool HasLocationSelection => activeSelection is LocationSelection;
    public bool HasFremenTroopSelection => activeSelection is FremenTroopSelection;
    public string? SelectedName => activeSelection?.Name;
    public string? SelectedType => activeSelection?.Type;
    public bool IsSelectionAtreides => activeSelection?.Controller == AreaController.Atreides;
    public bool IsSelectionDesert => activeSelection?.Controller == AreaController.Desert;
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

            LocationMarkers.Clear();
            foreach (var location in loaded.Locations)
            {
                LocationMarkers.Add(new LocationMarkerViewModel(location, SelectLocation));
            }

            FremenTroopMarkers.Clear();
            foreach (var troop in loaded.FremenTroops)
            {
                var location = loaded.FindFremenTroopLocation(troop.Id);
                if (location is not null)
                {
                    FremenTroopMarkers.Add(
                        new FremenTroopMarkerViewModel(troop, location, SelectFremenTroop));
                }
            }

            SetSelection(null);
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
        var nextSelection = marker is null
            || activeSelection is LocationSelection current
                && ReferenceEquals(current.Marker, marker)
            ? null
            : new LocationSelection(
                marker,
                new LocationDetailsViewModel(marker.Location, RefreshMapVisuals));
        SetSelection(nextSelection);
    }

    private void SelectFremenTroop(FremenTroopMarkerViewModel? marker)
    {
        var nextSelection = marker is null
            || activeSelection is FremenTroopSelection current
                && ReferenceEquals(current.Marker, marker)
            ? null
            : new FremenTroopSelection(
                marker,
                new FremenTroopDetailsViewModel(
                    marker.Troop,
                    marker.Location,
                    marker.RefreshDisplay));
        SetSelection(nextSelection);
    }

    private void SetSelection(IEditorSelection? selection)
    {
        activeSelection?.SetSelected(false);
        activeSelection = selection;
        activeSelection?.SetSelected(true);
        StatusText = activeSelection?.EditingStatus ?? GetIdleStatus();
        NotifySelectionChanged();
    }

    private string GetIdleStatus() => document is null
        ? "Open a Dune save to begin."
        : "Select a location or Fremen troop on the map.";

    [RelayCommand]
    private void CloseSelection() => SetSelection(null);

    private void RefreshMapVisuals()
    {
        if (document is null)
        {
            return;
        }

        foreach (var marker in LocationMarkers)
        {
            marker.IsVisible = SelectedMapFilter switch
            {
                MapFilter.Discovery => marker.Location.Discovered,
                _ => true,
            };
        }

        foreach (var marker in FremenTroopMarkers)
        {
            marker.IsVisible = IsTroopDisplayEnabled && (SelectedMapFilter switch
            {
                MapFilter.Discovery => marker.Location.Discovered,
                _ => true,
            });
        }

        if (activeSelection is { IsVisible: false })
        {
            SetSelection(null);
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

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedLocation));
        OnPropertyChanged(nameof(SelectedFremenTroop));
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

    private interface IEditorSelection
    {
        string Name { get; }
        string Type { get; }
        string EditingStatus { get; }
        AreaController Controller { get; }
        bool IsVisible { get; }
        void SetSelected(bool value);
    }

    private sealed record LocationSelection(
        LocationMarkerViewModel Marker,
        LocationDetailsViewModel Details) : IEditorSelection
    {
        public string Name => Details.Name;
        public string Type => Details.Type;
        public string EditingStatus => $"Editing {Marker.Name}.";
        public AreaController Controller => Marker.Location.Controller;
        public bool IsVisible => Marker.IsVisible;
        public void SetSelected(bool value) => Marker.IsSelected = value;
    }

    private sealed record FremenTroopSelection(
        FremenTroopMarkerViewModel Marker,
        FremenTroopDetailsViewModel Details) : IEditorSelection
    {
        public string Name => Details.Name;
        public string Type => Details.Type;
        public string EditingStatus =>
            $"Editing Fremen troop {Marker.Troop.Id.Value:D2} at {Marker.Location.Name}.";
        public AreaController Controller => Marker.Location.Controller;
        public bool IsVisible => Marker.IsVisible;
        public void SetSelected(bool value) => Marker.IsSelected = value;
    }
}
