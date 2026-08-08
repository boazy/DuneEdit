namespace DuneEdit.Core;

public enum AreaController
{
    Desert,
    Atreides,
    Harkonnen,
}


public sealed class Sietch
{
    public const int RecordSize = 0x1C;

    private const int StatusOffset = 0x0A;
    private readonly byte[] data;

    internal Sietch(ReadOnlySpan<byte> data)
    {
        if (data.Length != RecordSize)
        {
            throw new ArgumentException($"A location record must contain exactly {RecordSize} bytes.", nameof(data));
        }

        this.data = data.ToArray();
    }

    public byte RegionId { get => data[0x00]; set => data[0x00] = value; }
    public byte SubregionId { get => data[0x01]; set => data[0x01] = value; }
    public byte DesertAroundSietch { get => data[0x02]; set => data[0x02] = value; }
    public byte MapPosX { get => data[0x03]; set => data[0x03] = value; }
    public byte MapPosY { get => data[0x04]; set => data[0x04] = value; }
    public byte Unk05 { get => data[0x05]; set => data[0x05] = value; }
    public byte PosX { get => data[0x06]; set => data[0x06] = value; }
    public byte PosY { get => data[0x07]; set => data[0x07] = value; }
    public byte LocationType { get => data[0x08]; set => data[0x08] = value; }
    public byte PrimaryTroopId { get => data[0x09]; set => data[0x09] = value; }
    public byte Unk0B { get => data[0x0B]; set => data[0x0B] = value; }
    public byte Unk0C { get => data[0x0C]; set => data[0x0C] = value; }
    public byte Unk0D { get => data[0x0D]; set => data[0x0D] = value; }
    public byte Unk0E { get => data[0x0E]; set => data[0x0E] = value; }
    public byte Unk0F { get => data[0x0F]; set => data[0x0F] = value; }
    public byte SpiceFieldId { get => data[0x10]; set => data[0x10] = value; }
    public byte Spice { get => data[0x11]; set => data[0x11] = value; }
    public byte SpiceDensity { get => data[0x12]; set => data[0x12] = value; }
    public byte Unk13 { get => data[0x13]; set => data[0x13] = value; }
    public byte Harvesters { get => data[0x14]; set => data[0x14] = value; }
    public byte Ornis { get => data[0x15]; set => data[0x15] = value; }
    public byte Krys { get => data[0x16]; set => data[0x16] = value; }
    public byte Laserguns { get => data[0x17]; set => data[0x17] = value; }
    public byte WierdingModules { get => data[0x18]; set => data[0x18] = value; }
    public byte Atomics { get => data[0x19]; set => data[0x19] = value; }
    public byte Bulbs { get => data[0x1A]; set => data[0x1A] = value; }
    public byte Water { get => data[0x1B]; set => data[0x1B] = value; }

    public string Region => Regions.GetRegion(RegionId);
    public string Subregion => Regions.GetSubregion(SubregionId);
    public string Name => $"{Region} {Subregion}";

    public string LocationTypeGroup => LocationType switch
    {
        <= 0x10 => "Sietch",
        0x20 => "Carthag",
        0x21 => "Village",
        >= 0x22 and <= 0x2F => "Fort",
        0x30 => "Arrakeen",
        _ => "Unknown",
    };

    public string LocationTypeTitle => LocationTypeGroup switch
    {
        "Carthag" => "Carthag Palace",
        "Arrakeen" => "Arrakeen Palace",
        var title => $"{title}:",
    };

    public AreaController Controller => InventoryVisible
        ? AreaController.Atreides
        : LocationType is >= 0x28 and <= 0x30
            ? AreaController.Harkonnen
            : AreaController.Desert;

    public bool Vegetation { get => GetStatus(0); set => SetStatus(0, value); }
    public bool UnderAttack { get => GetStatus(1); set => SetStatus(1, value); }
    public bool Infiltrated { get => GetStatus(2); set => SetStatus(2, value); }
    public bool BattleWon { get => GetStatus(3); set => SetStatus(3, value); }
    public bool InventoryVisible { get => GetStatus(4); set => SetStatus(4, value); }
    public bool HasWindtrap { get => GetStatus(5); set => SetStatus(5, value); }
    public bool Prospected { get => GetStatus(6); set => SetStatus(6, value); }
    public bool Discovered { get => !GetStatus(7); set => SetStatus(7, !value); }

    internal void CopyTo(Span<byte> destination)
    {
        data.CopyTo(destination);
    }

    public byte[] ToArray() => (byte[])data.Clone();

    private bool GetStatus(int bit) => (data[StatusOffset] & (1 << bit)) != 0;

    private void SetStatus(int bit, bool enabled)
    {
        var mask = (byte)(1 << bit);
        data[StatusOffset] = enabled
            ? (byte)(data[StatusOffset] | mask)
            : (byte)(data[StatusOffset] & ~mask);
    }
}
