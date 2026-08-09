using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

public sealed class FremenTroopDetailsViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<TroopOccupation> canonicalOccupations =
    [
        TroopOccupation.Military,
        TroopOccupation.Ecology,
        TroopOccupation.Spice,
        TroopOccupation.Prospector,
        TroopOccupation.Unrecruited,
    ];

    private readonly Action? displayChanged;
    private TroopOccupationInfo occupationInfo;
    private TroopOccupation selectedOccupation;
    private TroopJob selectedJob;
    private TroopAllegiance selectedAllegiance;
    private bool jobCompleted;

    public FremenTroopDetailsViewModel(FremenTroop troop, Sietch location, Action? displayChanged = null)
    {
        Troop = troop;
        Location = location;
        this.displayChanged = displayChanged;
        occupationInfo = troop.OccupationInfo;
        selectedOccupation = occupationInfo.Occupation;
        selectedJob = occupationInfo.Job;
        selectedAllegiance = occupationInfo.Allegiance;
        jobCompleted = occupationInfo.JobCompleted;

        Attributes =
        [
            new("Motivation", () => troop.Motivation, value => troop.Motivation = value),
            new("Spice rank", () => troop.SpiceRank, value => troop.SpiceRank = value),
            new("Military rank", () => troop.ArmyRank, value => troop.ArmyRank = value),
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
    public Sietch Location { get; }
    public string Type => "Fremen troop";
    public string Name => $"Troop {Troop.Id:D2} · {Location.Name}";
    public IReadOnlyList<NumericFieldViewModel> Attributes { get; }
    public ScaledNumericFieldViewModel People { get; }
    public IReadOnlyList<BooleanFieldViewModel> Equipment { get; }
    public string? CurrentGameState => occupationInfo.CurrentGameState is { Length: > 0 } state ? state : null;
    public bool HasCurrentGameState => CurrentGameState is not null;
    public bool IsJobEnabled => !occupationInfo.IsUnknown && AvailableJobs.Count > 1;
    public bool IsAllegianceEnabled => !occupationInfo.IsUnknown && AvailableAllegiances.Count > 1;
    public bool IsJobCompletedEnabled => occupationInfo.IsJobCompletedApplicable;

    public IReadOnlyList<TroopOccupation> AvailableOccupations => occupationInfo.IsUnknown
        ? [TroopOccupation.Unknown, .. canonicalOccupations]
        : canonicalOccupations;

    public IReadOnlyList<TroopJob> AvailableJobs => occupationInfo.IsUnknown
        ? []
        : TroopOccupationInfo.GetAllowedJobs(selectedOccupation, selectedAllegiance);

    public IReadOnlyList<TroopAllegiance> AvailableAllegiances => occupationInfo.IsUnknown
        ? []
        : TroopOccupationInfo.GetAllowedAllegiances(selectedOccupation);

    public TroopOccupation SelectedOccupation
    {
        get => selectedOccupation;
        set
        {
            if (!SetProperty(ref selectedOccupation, value) || value == TroopOccupation.Unknown)
            {
                return;
            }

            selectedAllegiance = TroopOccupationInfo.GetAllowedAllegiances(value)[0];
            selectedJob = TroopOccupationInfo.GetAllowedJobs(value, selectedAllegiance)[0];
            jobCompleted = false;
            ApplyEditedOccupation();
            NotifyOccupationControlsChanged();
        }
    }

    public TroopJob SelectedJob
    {
        get => selectedJob;
        set
        {
            if (!SetProperty(ref selectedJob, value) || occupationInfo.IsUnknown)
            {
                return;
            }

            ApplyEditedOccupation();
        }
    }

    public TroopAllegiance SelectedAllegiance
    {
        get => selectedAllegiance;
        set
        {
            if (!SetProperty(ref selectedAllegiance, value) || occupationInfo.IsUnknown)
            {
                return;
            }

            selectedJob = TroopOccupationInfo.GetAllowedJobs(selectedOccupation, value)[0];
            ApplyEditedOccupation();
            NotifyOccupationControlsChanged();
        }
    }

    public bool JobCompleted
    {
        get => jobCompleted;
        set
        {
            if (!SetProperty(ref jobCompleted, value) || occupationInfo.IsUnknown)
            {
                return;
            }

            ApplyEditedOccupation();
        }
    }

    private void ApplyEditedOccupation()
    {
        occupationInfo = TroopOccupationInfo.CreateEdited(
            selectedOccupation,
            selectedJob,
            jobCompleted,
            selectedAllegiance);
        Troop.ApplyOccupationInfo(occupationInfo);
        OnPropertyChanged(nameof(CurrentGameState));
        OnPropertyChanged(nameof(HasCurrentGameState));
        OnPropertyChanged(nameof(IsJobCompletedEnabled));
        displayChanged?.Invoke();
    }

    private void NotifyOccupationControlsChanged()
    {
        OnPropertyChanged(nameof(AvailableOccupations));
        OnPropertyChanged(nameof(AvailableJobs));
        OnPropertyChanged(nameof(AvailableAllegiances));
        OnPropertyChanged(nameof(SelectedJob));
        OnPropertyChanged(nameof(SelectedAllegiance));
        OnPropertyChanged(nameof(JobCompleted));
        OnPropertyChanged(nameof(IsJobEnabled));
        OnPropertyChanged(nameof(IsAllegianceEnabled));
        OnPropertyChanged(nameof(IsJobCompletedEnabled));
    }
}
