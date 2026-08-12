namespace DuneEdit.Core;

public readonly record struct LocationId
{
    public LocationId(byte regionId, byte subregionId)
    {
        if (regionId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(regionId), "A location region ID cannot be zero.");
        }

        if (subregionId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subregionId), "A location subregion ID cannot be zero.");
        }

        RegionId = regionId;
        SubregionId = subregionId;
    }

    public byte RegionId { get; }
    public byte SubregionId { get; }
}

public readonly record struct TroopId
{
    public TroopId(byte value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "A troop ID cannot be zero.");
        }

        Value = value;
    }

    public byte Value { get; }
}

public readonly record struct MapPosition(byte EncodedX, byte EncodedY);

public readonly record struct MapPoint(double X, double Y);

public static class MapProjection
{
    public static MapPoint Project(
        MapPosition position,
        double width,
        double height,
        double margin)
    {
        var usableWidth = width - (margin * 2);
        var usableHeight = height - (margin * 2);
        var encodedLatitude = position.EncodedY > 180
            ? position.EncodedY - 180
            : position.EncodedY + 75;
        return new MapPoint(
            margin + Math.Round((position.EncodedX / (double)byte.MaxValue) * usableWidth),
            margin + Math.Round((encodedLatitude / 150d) * usableHeight));
    }

    public static MapPoint GetTroopOffset(TroopPlacement placement) => placement switch
    {
        TroopPlacement.South => new(0, 13),
        TroopPlacement.SouthEast => new(11, 10),
        TroopPlacement.SouthWest => new(-11, 10),
        TroopPlacement.East => new(14, 0),
        TroopPlacement.West => new(-14, 0),
        TroopPlacement.NorthEast => new(11, -10),
        TroopPlacement.NorthWest => new(-11, -10),
        TroopPlacement.North => new(0, -13),
        _ => new(0, 0),
    };
}

public enum LocationKind
{
    Unknown,
    Sietch,
    Village,
    Fort,
    CarthagPalace,
    ArrakeenPalace,
}

public enum TroopPlacement
{
    Unknown = 0,
    South = 1,
    SouthEast = 2,
    SouthWest = 3,
    East = 4,
    West = 5,
    NorthEast = 6,
    NorthWest = 7,
    North = 8,
}
