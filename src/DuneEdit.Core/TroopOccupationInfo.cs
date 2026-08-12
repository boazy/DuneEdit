namespace DuneEdit.Core;

public enum TroopOccupation
{
    Military,
    Ecology,
    Spice,
    Prospector,
    Unrecruited,
    Unknown,
}

public enum TroopJob
{
    None,
    Training,
    Espionage,
    Attacking,
    IrrigationAndTreeCare,
    WindtrapAssembly,
    BulbGrowing,
    Mining,
    Prospecting,
    WaitingForOrders,
    SearchingForEquipment,
}

public enum TroopAllegiance
{
    Atreides,
    Harkonnen,
    None,
}

public readonly record struct TroopOccupationInfo(
    TroopOccupation Occupation,
    TroopJob Job,
    bool JobCompleted,
    TroopAllegiance Allegiance,
    byte RawJobCode)
{
    public bool IsUnknown => Occupation == TroopOccupation.Unknown;
    public bool IsJobCompletedApplicable => !IsUnknown && Occupation != TroopOccupation.Unrecruited;

    public static TroopOccupationInfo FromRawJobCode(byte rawJobCode)
    {
        if (rawJobCode is >= 128 and <= 159)
        {
            return new(TroopOccupation.Unrecruited, TroopJob.None, false, TroopAllegiance.None, rawJobCode);
        }

        if (rawJobCode is >= 64 and <= 127)
        {
            return Unknown(rawJobCode);
        }

        if (rawJobCode >= 160)
        {
            return Unknown(rawJobCode);
        }

        if (rawJobCode is >= 32 and <= 35)
        {
            return Unknown(rawJobCode);
        }

        var completed = rawJobCode is >= 16 and <= 31;
        var baseJobCode = completed ? (byte)(rawJobCode - 16) : rawJobCode;
        return baseJobCode switch
        {
            0 => Known(TroopOccupation.Spice, TroopJob.Mining, completed, TroopAllegiance.Atreides, rawJobCode),
            1 => Known(TroopOccupation.Prospector, TroopJob.Prospecting, completed, TroopAllegiance.Atreides, rawJobCode),
            3 => Known(TroopOccupation.Spice, TroopJob.SearchingForEquipment, completed, TroopAllegiance.Atreides, rawJobCode),
            4 => Known(TroopOccupation.Military, TroopJob.Training, completed, TroopAllegiance.Atreides, rawJobCode),
            5 => Known(TroopOccupation.Military, TroopJob.Espionage, completed, TroopAllegiance.Atreides, rawJobCode),
            6 => Known(TroopOccupation.Military, TroopJob.Attacking, completed, TroopAllegiance.Atreides, rawJobCode),
            7 => Known(TroopOccupation.Military, TroopJob.SearchingForEquipment, completed, TroopAllegiance.Atreides, rawJobCode),
            8 => Known(TroopOccupation.Ecology, TroopJob.IrrigationAndTreeCare, completed, TroopAllegiance.Atreides, rawJobCode),
            9 => Known(TroopOccupation.Ecology, TroopJob.WindtrapAssembly, completed, TroopAllegiance.Atreides, rawJobCode),
            10 => Known(TroopOccupation.Ecology, TroopJob.BulbGrowing, completed, TroopAllegiance.Atreides, rawJobCode),
            11 => Known(TroopOccupation.Ecology, TroopJob.SearchingForEquipment, completed, TroopAllegiance.Atreides, rawJobCode),
            12 => Known(TroopOccupation.Spice, TroopJob.Mining, completed, TroopAllegiance.Harkonnen, rawJobCode),
            13 => Known(TroopOccupation.Prospector, TroopJob.Prospecting, completed, TroopAllegiance.Harkonnen, rawJobCode),
            15 => Known(TroopOccupation.Spice, TroopJob.SearchingForEquipment, completed, TroopAllegiance.Harkonnen, rawJobCode),
            _ => Unknown(rawJobCode),
        };
    }

    public static TroopOccupationInfo CreateEdited(
        TroopOccupation occupation,
        TroopJob job,
        bool jobCompleted,
        TroopAllegiance allegiance)
    {
        var rawJobCode = Encode(occupation, job, jobCompleted, allegiance);
        return new(occupation, job, jobCompleted, allegiance, rawJobCode);
    }

    public static IReadOnlyList<TroopJob> GetAllowedJobs(TroopOccupation occupation, TroopAllegiance allegiance) =>
        (occupation, allegiance) switch
        {
            (TroopOccupation.Spice, TroopAllegiance.Atreides or TroopAllegiance.Harkonnen) => [TroopJob.Mining, TroopJob.SearchingForEquipment],
            (TroopOccupation.Prospector, TroopAllegiance.Atreides or TroopAllegiance.Harkonnen) => [TroopJob.Prospecting],
            (TroopOccupation.Military, TroopAllegiance.Atreides) => [TroopJob.Training, TroopJob.Espionage, TroopJob.Attacking, TroopJob.SearchingForEquipment],
            (TroopOccupation.Ecology, TroopAllegiance.Atreides) => [TroopJob.IrrigationAndTreeCare, TroopJob.WindtrapAssembly, TroopJob.BulbGrowing, TroopJob.SearchingForEquipment],
            (TroopOccupation.Unrecruited, TroopAllegiance.None) => [TroopJob.None],
            _ => [],
        };

    public static IReadOnlyList<TroopAllegiance> GetAllowedAllegiances(TroopOccupation occupation) => occupation switch
    {
        TroopOccupation.Spice or TroopOccupation.Prospector => [TroopAllegiance.Atreides, TroopAllegiance.Harkonnen],
        TroopOccupation.Military or TroopOccupation.Ecology => [TroopAllegiance.Atreides],
        TroopOccupation.Unrecruited => [TroopAllegiance.None],
        _ => [],
    };

    public TroopOccupationInfo WithOccupation(TroopOccupation occupation)
    {
        if (occupation == Occupation)
        {
            return this;
        }

        var allegiances = GetAllowedAllegiances(occupation);
        if (allegiances.Count == 0)
        {
            throw new ArgumentException("The occupation has no encodable allegiance.", nameof(occupation));
        }

        var allegiance = allegiances[0];
        var job = GetAllowedJobs(occupation, allegiance)[0];
        return CreateEdited(occupation, job, jobCompleted: false, allegiance);
    }

    public TroopOccupationInfo WithJob(TroopJob job) =>
        CreateEdited(Occupation, job, JobCompleted, Allegiance);

    public TroopOccupationInfo WithAllegiance(TroopAllegiance allegiance)
    {
        var job = GetAllowedJobs(Occupation, allegiance)[0];
        return CreateEdited(Occupation, job, JobCompleted, allegiance);
    }

    public TroopOccupationInfo WithJobCompleted(bool jobCompleted) =>
        CreateEdited(Occupation, Job, jobCompleted, Allegiance);

    public string CurrentGameState => RawJobCode switch
    {
        2 or 18 => "Waiting for orders (occupation not encoded)",
        14 or 30 => "Harkonnen waiting for orders (occupation not encoded)",
        32 => "Spice mining: no more orders",
        33 => "Captured prospector: no more orders",
        34 => "Captured troop apology",
        35 => "Spice harvester search: no more orders",
        >= 64 and <= 127 => "Moving to another location",
        >= 128 and <= 159 => "Not yet recruited",
        >= 160 => "Complaint about Harkonnen slavery",
        _ => string.Empty,
    };

    private static TroopOccupationInfo Known(
        TroopOccupation occupation,
        TroopJob job,
        bool jobCompleted,
        TroopAllegiance allegiance,
        byte rawJobCode) => new(occupation, job, jobCompleted, allegiance, rawJobCode);

    private static TroopOccupationInfo Unknown(byte rawJobCode) =>
        new(TroopOccupation.Unknown, TroopJob.None, false, TroopAllegiance.None, rawJobCode);

    private static byte Encode(
        TroopOccupation occupation,
        TroopJob job,
        bool jobCompleted,
        TroopAllegiance allegiance)
    {
        if (occupation == TroopOccupation.Unrecruited && job == TroopJob.None && allegiance == TroopAllegiance.None)
        {
            return 128;
        }

        var baseJobCode = (occupation, job, allegiance) switch
        {
            (TroopOccupation.Spice, TroopJob.Mining, TroopAllegiance.Atreides) => 0,
            (TroopOccupation.Spice, TroopJob.SearchingForEquipment, TroopAllegiance.Atreides) => 3,
            (TroopOccupation.Prospector, TroopJob.Prospecting, TroopAllegiance.Atreides) => 1,
            (TroopOccupation.Military, TroopJob.Training, TroopAllegiance.Atreides) => 4,
            (TroopOccupation.Military, TroopJob.Espionage, TroopAllegiance.Atreides) => 5,

            (TroopOccupation.Military, TroopJob.Attacking, TroopAllegiance.Atreides) => 6,
            (TroopOccupation.Military, TroopJob.SearchingForEquipment, TroopAllegiance.Atreides) => 7,
            (TroopOccupation.Ecology, TroopJob.IrrigationAndTreeCare, TroopAllegiance.Atreides) => 8,
            (TroopOccupation.Ecology, TroopJob.WindtrapAssembly, TroopAllegiance.Atreides) => 9,
            (TroopOccupation.Ecology, TroopJob.BulbGrowing, TroopAllegiance.Atreides) => 10,
            (TroopOccupation.Ecology, TroopJob.SearchingForEquipment, TroopAllegiance.Atreides) => 11,
            (TroopOccupation.Spice, TroopJob.Mining, TroopAllegiance.Harkonnen) => 12,
            (TroopOccupation.Spice, TroopJob.SearchingForEquipment, TroopAllegiance.Harkonnen) => 15,
            (TroopOccupation.Prospector, TroopJob.Prospecting, TroopAllegiance.Harkonnen) => 13,
            _ => throw new ArgumentException("The occupation, job, and allegiance combination is not encodable.")
        };

        return (byte)(baseJobCode + (jobCompleted ? 16 : 0));
    }
}
public static class TroopOccupationDisplay
{
    public static string ToDisplayName(this TroopOccupation occupation) => occupation switch
    {
        TroopOccupation.Military => "Military",
        TroopOccupation.Ecology => "Ecology",
        TroopOccupation.Spice => "Spice",
        TroopOccupation.Prospector => "Prospectors",
        TroopOccupation.Unrecruited => "Unrecruited",
        TroopOccupation.Unknown => "Unknown — preserve raw state",
        _ => throw new ArgumentOutOfRangeException(nameof(occupation), occupation, null),
    };

    public static string ToDisplayName(this TroopJob job) => job switch
    {
        TroopJob.None => "None",
        TroopJob.Training => "Training",
        TroopJob.Espionage => "Espionage",
        TroopJob.Attacking => "Attacking",
        TroopJob.IrrigationAndTreeCare => "Irrigation and tree care",
        TroopJob.WindtrapAssembly => "Windtrap assembly",
        TroopJob.BulbGrowing => "Bulb growing",
        TroopJob.Mining => "Mining",
        TroopJob.Prospecting => "Prospecting",
        TroopJob.WaitingForOrders => "Waiting for orders",
        TroopJob.SearchingForEquipment => "Searching for equipment",
        _ => throw new ArgumentOutOfRangeException(nameof(job), job, null),
    };

    public static string ToDisplayName(this TroopAllegiance allegiance) => allegiance switch
    {
        TroopAllegiance.Atreides => "Atreides",
        TroopAllegiance.Harkonnen => "Harkonnen",
        TroopAllegiance.None => "None",
        _ => throw new ArgumentOutOfRangeException(nameof(allegiance), allegiance, null),
    };
}
