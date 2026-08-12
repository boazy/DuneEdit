using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

public sealed class FremenTroopDetailsViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<TroopOccupation> CanonicalOccupations =
    [
        TroopOccupation.Military,
        TroopOccupation.Ecology,
        TroopOccupation.Spice,
        TroopOccupation.Prospector,
        TroopOccupation.Unrecruited,
    ];

    private readonly Action? displayChanged;
    private TroopOccupationInfo occupationInfo;

    public FremenTroopDetailsViewModel(FremenTroop troop, DuneLocation location, Action? displayChanged = null)
    {
        Troop = troop;
        Location = location;
        this.displayChanged = displayChanged;
        occupationInfo = troop.OccupationInfo;

        Attributes =
        [
            new("Motivation", () => troop.Motivation, value => troop.Motivation = value),
            new("Spice rank", () => troop.SpiceRank, value => troop.SpiceRank = value),
            new("Military rank", () => troop.MilitaryRank, value => troop.MilitaryRank = value),
            new("Ecology rank", () => troop.EcologyRank, value => troop.EcologyRank = value),
        ];

        People = new("People", () => troop.People, value => troop.People = value, 2550, 10);

        Equipment =
        [
            new("Harvester", () => troop.HasHarvesters, value => troop.HasHarvesters = value),
            new("Ornithopter", () => troop.HasOrnithopters, value => troop.HasOrnithopters = value),
            new("Krys knife", () => troop.HasKrysKnives, value => troop.HasKrysKnives = value),
            new("Lasergun", () => troop.HasLaserguns, value => troop.HasLaserguns = value),
            new("Weirding module", () => troop.HasWeirdingModules, value => troop.HasWeirdingModules = value),
            new("Atomics", () => troop.HasAtomics, value => troop.HasAtomics = value),
            new("Bulbs", () => troop.HasBulbs, value => troop.HasBulbs = value),
        ];
    }

    public FremenTroop Troop { get; }
    public DuneLocation Location { get; }
    public string Type => "Fremen troop";
    public string Name => $"Troop {Troop.Id.Value:D2} · {Location.Name}";
    public IReadOnlyList<NumericFieldViewModel> Attributes { get; }
    public ScaledNumericFieldViewModel People { get; }
    public IReadOnlyList<BooleanFieldViewModel> Equipment { get; }
    public string? CurrentGameState => occupationInfo.CurrentGameState is { Length: > 0 } state ? state : null;
    public bool HasCurrentGameState => CurrentGameState is not null;
    public bool IsJobEnabled => !occupationInfo.IsUnknown && AvailableJobs.Count > 1;
    public bool IsAllegianceEnabled => !occupationInfo.IsUnknown && AvailableAllegiances.Count > 1;
    public bool IsJobCompletedEnabled => occupationInfo.IsJobCompletedApplicable;

    public IReadOnlyList<TroopOccupation> AvailableOccupations => occupationInfo.IsUnknown
        ? [TroopOccupation.Unknown, .. CanonicalOccupations]
        : CanonicalOccupations;

    public IReadOnlyList<TroopJob> AvailableJobs => occupationInfo.IsUnknown
        ? []
        : TroopOccupationInfo.GetAllowedJobs(occupationInfo.Occupation, occupationInfo.Allegiance);

    public IReadOnlyList<TroopAllegiance> AvailableAllegiances => occupationInfo.IsUnknown
        ? []
        : TroopOccupationInfo.GetAllowedAllegiances(occupationInfo.Occupation);

    public TroopOccupation SelectedOccupation
    {
        get => occupationInfo.Occupation;
        set
        {
            if (value == occupationInfo.Occupation || value == TroopOccupation.Unknown)
            {
                return;
            }

            ApplyEditedOccupation(occupationInfo.WithOccupation(value));
        }
    }

    public TroopJob SelectedJob
    {
        get => occupationInfo.Job;
        set
        {
            if (value == occupationInfo.Job || occupationInfo.IsUnknown)
            {
                return;
            }

            ApplyEditedOccupation(occupationInfo.WithJob(value));
        }
    }

    public TroopAllegiance SelectedAllegiance
    {
        get => occupationInfo.Allegiance;
        set
        {
            if (value == occupationInfo.Allegiance || occupationInfo.IsUnknown)
            {
                return;
            }

            ApplyEditedOccupation(occupationInfo.WithAllegiance(value));
        }
    }

    public bool JobCompleted
    {
        get => occupationInfo.JobCompleted;
        set
        {
            if (value == occupationInfo.JobCompleted || occupationInfo.IsUnknown)
            {
                return;
            }

            ApplyEditedOccupation(occupationInfo.WithJobCompleted(value));
        }
    }

    private void ApplyEditedOccupation(TroopOccupationInfo edited)
    {
        occupationInfo = edited;
        Troop.ApplyOccupationInfo(occupationInfo);
        NotifyOccupationControlsChanged();
        OnPropertyChanged(nameof(CurrentGameState));
        OnPropertyChanged(nameof(HasCurrentGameState));
        displayChanged?.Invoke();
    }

    private void NotifyOccupationControlsChanged()
    {
        OnPropertyChanged(nameof(AvailableOccupations));
        OnPropertyChanged(nameof(AvailableJobs));
        OnPropertyChanged(nameof(AvailableAllegiances));
        OnPropertyChanged(nameof(SelectedOccupation));
        OnPropertyChanged(nameof(SelectedJob));
        OnPropertyChanged(nameof(SelectedAllegiance));
        OnPropertyChanged(nameof(JobCompleted));
        OnPropertyChanged(nameof(IsJobEnabled));
        OnPropertyChanged(nameof(IsAllegianceEnabled));
        OnPropertyChanged(nameof(IsJobCompletedEnabled));
    }
}
