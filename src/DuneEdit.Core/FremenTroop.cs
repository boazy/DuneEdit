namespace DuneEdit.Core;

public sealed class FremenTroop
{
    public const int RecordSize = 0x1B;

    private readonly byte[] data;

    internal FremenTroop(ReadOnlySpan<byte> data)
    {
        if (data.Length != RecordSize)
        {
            throw new ArgumentException($"A Fremen troop record must be {RecordSize} bytes.", nameof(data));
        }

        this.data = data.ToArray();
    }

    public TroopId Id => new(data[0x00]);
    public TroopId? NextTroopId => data[0x01] == 0 ? null : new TroopId(data[0x01]);
    public TroopPlacement Placement => Enum.IsDefined((TroopPlacement)data[0x02])
        ? (TroopPlacement)data[0x02]
        : TroopPlacement.Unknown;
    public byte RawJobCode { get => data[0x03]; set => data[0x03] = value; }
    public bool IsRecruited => RawJobCode is < 128 or > 159;
    public TroopOccupationInfo OccupationInfo => TroopOccupationInfo.FromRawJobCode(RawJobCode);
    public FremenTroopRole Role => OccupationInfo.Occupation switch
    {
        TroopOccupation.Spice => FremenTroopRole.Spice,
        TroopOccupation.Prospector => FremenTroopRole.Prospector,
        TroopOccupation.Ecology => FremenTroopRole.Ecology,
        _ => FremenTroopRole.Military,
    };

    public void ApplyOccupationInfo(TroopOccupationInfo occupationInfo) =>
        RawJobCode = occupationInfo.RawJobCode;

    public byte Motivation { get => data[0x15]; set => data[0x15] = value; }
    public byte SpiceRank { get => data[0x16]; set => data[0x16] = value; }
    public byte MilitaryRank { get => data[0x17]; set => data[0x17] = value; }
    public byte EcologyRank { get => data[0x18]; set => data[0x18] = value; }
    public byte PopulationTens { get => data[0x1A]; set => data[0x1A] = value; }
    public int People
    {
        get => PopulationTens * 10;
        set => PopulationTens = (byte)Math.Clamp((int)Math.Round(value / 10d, MidpointRounding.AwayFromZero), byte.MinValue, byte.MaxValue);
    }

    public bool HasHarvesters { get => GetEquipment(7); set => SetEquipment(7, value); }
    public bool HasOrnithopters { get => GetEquipment(6); set => SetEquipment(6, value); }
    public bool HasKrysKnives { get => GetEquipment(5); set => SetEquipment(5, value); }
    public bool HasLaserguns { get => GetEquipment(4); set => SetEquipment(4, value); }
    public bool HasWeirdingModules { get => GetEquipment(3); set => SetEquipment(3, value); }
    public bool HasAtomics { get => GetEquipment(2); set => SetEquipment(2, value); }
    public bool HasBulbs { get => GetEquipment(1); set => SetEquipment(1, value); }

    internal void CopyTo(Span<byte> destination) => data.CopyTo(destination);

    public byte[] ToArray() => (byte[])data.Clone();

    private bool GetEquipment(int bit) => (data[0x19] & (1 << bit)) != 0;

    private void SetEquipment(int bit, bool enabled)
    {
        if (enabled)
        {
            data[0x19] |= (byte)(1 << bit);
        }
        else
        {
            data[0x19] &= (byte)~(1 << bit);
        }
    }
}
