using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

public sealed class LocationDetailsViewModel : ViewModelBase
{
    public LocationDetailsViewModel(Sietch location, Action? mapVisualChanged = null)
    {
        Location = location;

        Resources =
        [
            new("Spice", () => location.Spice, value => location.Spice = value),
            new(
                "Spice density",
                () => location.SpiceDensity,
                value => location.SpiceDensity = value,
                mapVisualChanged),
            new("Harvesters", () => location.Harvesters, value => location.Harvesters = value),
            new("Ornis", () => location.Ornis, value => location.Ornis = value),
            new("Krys", () => location.Krys, value => location.Krys = value),
            new("Laserguns", () => location.Laserguns, value => location.Laserguns = value),
            new("Weirding modules", () => location.WierdingModules, value => location.WierdingModules = value),
            new("Atomics", () => location.Atomics, value => location.Atomics = value),
            new("Bulbs", () => location.Bulbs, value => location.Bulbs = value),
            new("Water", () => location.Water, value => location.Water = value),
        ];

        Conditions =
        [
            new("Has vegetation", () => location.Vegetation, value => location.Vegetation = value),
            new("Under attack", () => location.UnderAttack, value => location.UnderAttack = value),
            new("Infiltrated", () => location.Infiltrated, value => location.Infiltrated = value),
            new("Battle won", () => location.BattleWon, value => location.BattleWon = value),
            new(
                "Inventory visible",
                () => location.InventoryVisible,
                value => location.InventoryVisible = value,
                () =>
                {
                    OnPropertyChanged(nameof(IsAtreidesControlled));
                    OnPropertyChanged(nameof(IsDesertControlled));
                    mapVisualChanged?.Invoke();
                }),
            new("Has windtrap", () => location.HasWindtrap, value => location.HasWindtrap = value),
            new("Prospected", () => location.Prospected, value => location.Prospected = value),
            new(
                "Discovered",
                () => location.Discovered,
                value => location.Discovered = value,
                mapVisualChanged),
        ];

        Advanced =
        [
            new("Map X position", () => location.MapPosX, value => location.MapPosX = value),
            new("Map Y position", () => location.MapPosY, value => location.MapPosY = value),
            new("Desert around", () => location.DesertAroundSietch, value => location.DesertAroundSietch = value),
            new(
                "Location type",
                () => location.LocationType,
                value => location.LocationType = value,
                () =>
                {
                    OnPropertyChanged(nameof(IsAtreidesControlled));
                    OnPropertyChanged(nameof(IsDesertControlled));
                    mapVisualChanged?.Invoke();
                }),
        ];

        Unknown =
        [
            new("Position X", () => location.PosX, value => location.PosX = value),
            new("Position Y", () => location.PosY, value => location.PosY = value),
            new(
                "Spice field",
                () => location.SpiceFieldId,
                value => location.SpiceFieldId = value,
                mapVisualChanged),
            new("Unknown 05", () => location.Unk05, value => location.Unk05 = value),
            new("Unknown 0B", () => location.Unk0B, value => location.Unk0B = value),
            new("Unknown 0C", () => location.Unk0C, value => location.Unk0C = value),
            new("Unknown 0D", () => location.Unk0D, value => location.Unk0D = value),
            new("Unknown 0E", () => location.Unk0E, value => location.Unk0E = value),
            new("Unknown 0F", () => location.Unk0F, value => location.Unk0F = value),
            new("Unknown 13", () => location.Unk13, value => location.Unk13 = value),
        ];
    }

    public Sietch Location { get; }
    public string Name => Location.Name;
    public string Type => Location.LocationTypeTitle.TrimEnd(':');
    public bool IsAtreidesControlled => Location.Controller == AreaController.Atreides;
    public bool IsDesertControlled => Location.Controller == AreaController.Desert;
    public IReadOnlyList<NumericFieldViewModel> Resources { get; }
    public IReadOnlyList<BooleanFieldViewModel> Conditions { get; }
    public IReadOnlyList<NumericFieldViewModel> Advanced { get; }
    public IReadOnlyList<NumericFieldViewModel> Unknown { get; }
}
