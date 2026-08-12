using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

public sealed class LocationDetailsViewModel : ViewModelBase
{
    public LocationDetailsViewModel(DuneLocation location, Action? mapVisualChanged = null)
    {
        Location = location;
        Resources = CreateResourceFields(location, mapVisualChanged);
        Conditions = CreateConditionFields(location, mapVisualChanged);
        Advanced = CreateAdvancedFields(location, mapVisualChanged);
        Unknown = CreateUnknownFields(location, mapVisualChanged);
    }

    public DuneLocation Location { get; }
    public string Name => Location.Name;
    public string Type => Location.Kind.ToDisplayTitle();
    public bool IsAtreidesControlled => Location.Controller == AreaController.Atreides;
    public bool IsDesertControlled => Location.Controller == AreaController.Desert;
    public IReadOnlyList<NumericFieldViewModel> Resources { get; }
    public IReadOnlyList<BooleanFieldViewModel> Conditions { get; }
    public IReadOnlyList<NumericFieldViewModel> Advanced { get; }
    public IReadOnlyList<NumericFieldViewModel> Unknown { get; }

    private static IReadOnlyList<NumericFieldViewModel> CreateResourceFields(
        DuneLocation location,
        Action? mapVisualChanged) =>
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

    private IReadOnlyList<BooleanFieldViewModel> CreateConditionFields(
        DuneLocation location,
        Action? mapVisualChanged) =>
    [
        new("Has vegetation", () => location.Vegetation, value => location.Vegetation = value),
        new("Under attack", () => location.UnderAttack, value => location.UnderAttack = value),
        new("Infiltrated", () => location.Infiltrated, value => location.Infiltrated = value),
        new("Battle won", () => location.BattleWon, value => location.BattleWon = value),
        new(
            "Inventory visible",
            () => location.InventoryVisible,
            value => location.InventoryVisible = value,
            () => NotifyControlChanged(mapVisualChanged)),
        new("Has windtrap", () => location.HasWindtrap, value => location.HasWindtrap = value),
        new("Prospected", () => location.Prospected, value => location.Prospected = value),
        new(
            "Discovered",
            () => location.Discovered,
            value => location.Discovered = value,
            mapVisualChanged),
    ];

    private IReadOnlyList<NumericFieldViewModel> CreateAdvancedFields(
        DuneLocation location,
        Action? mapVisualChanged) =>
    [
        new(
            "Map X position",
            () => location.MapPosition.EncodedX,
            value => location.MapPosition = location.MapPosition with { EncodedX = value }),
        new(
            "Map Y position",
            () => location.MapPosition.EncodedY,
            value => location.MapPosition = location.MapPosition with { EncodedY = value }),
        new("Desert around", () => location.DesertAround, value => location.DesertAround = value),
        new(
            "Location type",
            () => location.RawTypeCode,
            value => location.RawTypeCode = value,
            () => NotifyControlChanged(mapVisualChanged)),
    ];

    private static IReadOnlyList<NumericFieldViewModel> CreateUnknownFields(
        DuneLocation location,
        Action? mapVisualChanged) =>
    [
        new("Local position X", () => location.LocalPositionX, value => location.LocalPositionX = value),
        new("Local position Y", () => location.LocalPositionY, value => location.LocalPositionY = value),
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

    private void NotifyControlChanged(Action? mapVisualChanged)
    {
        OnPropertyChanged(nameof(Type));
        OnPropertyChanged(nameof(IsAtreidesControlled));
        OnPropertyChanged(nameof(IsDesertControlled));
        mapVisualChanged?.Invoke();
    }
}
