namespace DuneEdit.Core;

public enum SavegameFormat
{
    CompressedSave,
    Executable,
}

public sealed class DuneSavegame
{
    private const int FremenTroopSlots = 68;
    private readonly byte[] decompressedData;
    private readonly int fremenTroopsOffset;
    private readonly int locationsOffset;
    private readonly Dictionary<TroopId, DuneLocation> locationsByFremenTroopId;
    private readonly Dictionary<LocationId, DuneLocation> locationsById;

    private DuneSavegame(
        byte[] decompressedData,
        SavegameFormat format,
        int locationCount,
        int locationsOffset,
        string? sourcePath)
    {
        this.decompressedData = decompressedData;
        this.locationsOffset = locationsOffset;
        Format = format;
        SourcePath = sourcePath;

        var locations = ParseLocations(decompressedData, locationsOffset, locationCount);
        Locations = Array.AsReadOnly(locations);
        locationsById = locations.ToDictionary(location => location.Id);

        fremenTroopsOffset = locationsOffset + (locations.Length * DuneLocation.RecordSize) + 2;
        var fremenTroops = ParseFremenTroops(decompressedData, fremenTroopsOffset);
        FremenTroops = Array.AsReadOnly(fremenTroops);
        locationsByFremenTroopId = IndexTroopLocations(locations, fremenTroops);
    }

    public SavegameFormat Format { get; }
    public string? SourcePath { get; private set; }
    public IReadOnlyList<FremenTroop> FremenTroops { get; }
    public IReadOnlyList<DuneLocation> Locations { get; }

    public static DuneSavegame Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var format = string.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase)
            ? SavegameFormat.Executable
            : SavegameFormat.CompressedSave;

        return Parse(File.ReadAllBytes(filePath), format, Path.GetFullPath(filePath));
    }

    public static DuneSavegame Parse(
        ReadOnlySpan<byte> fileData,
        SavegameFormat format,
        string? sourcePath = null)
    {
        var signatures = format == SavegameFormat.CompressedSave
            ? LocationSignatures.CompressedSave
            : LocationSignatures.Executable;
        var decompressed = format == SavegameFormat.CompressedSave
            ? DecompressSave(fileData)
            : fileData.ToArray();
        var offset = FindLocationsOffset(decompressed, signatures);
        return new DuneSavegame(decompressed, format, signatures.Length - 1, offset, sourcePath);
    }

    public DuneLocation? FindLocation(LocationId id) =>
        locationsById.GetValueOrDefault(id);

    public DuneLocation? FindFremenTroopLocation(TroopId troopId) =>
        locationsByFremenTroopId.GetValueOrDefault(troopId);

    public byte[] ToDecompressedBytes()
    {
        var output = (byte[])decompressedData.Clone();
        CopyLocationsTo(output);
        CopyFremenTroopsTo(output);
        return output;
    }

    public byte[] ToFileBytes()
    {
        var output = ToDecompressedBytes();
        return Format == SavegameFormat.CompressedSave
            ? SavegameCompression.Compress(output)
            : output;
    }

    public void Save(string? filePath = null)
    {
        var targetPath = filePath ?? SourcePath;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new InvalidOperationException("A destination path is required for a save that has no source file.");
        }

        var fullPath = Path.GetFullPath(targetPath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The destination path has no parent directory.");
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllBytes(temporaryPath, ToFileBytes());
            File.Move(temporaryPath, fullPath, overwrite: true);
            SourcePath = fullPath;
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static byte[] DecompressSave(ReadOnlySpan<byte> fileData)
    {
        var declaredLength = SavegameCompression.ReadDeclaredFileLength(fileData);
        if (declaredLength != fileData.Length)
        {
            throw new InvalidDataException(
                $"The save header declares {declaredLength} bytes, but the file contains {fileData.Length} bytes.");
        }

        return SavegameCompression.Decompress(fileData);
    }

    private static DuneLocation[] ParseLocations(byte[] data, int offset, int count) =>
        Enumerable.Range(0, count)
            .Select(index => new DuneLocation(
                data.AsSpan(offset + (index * DuneLocation.RecordSize), DuneLocation.RecordSize)))
            .ToArray();

    private static FremenTroop[] ParseFremenTroops(byte[] data, int offset)
    {
        if (offset + (FremenTroopSlots * FremenTroop.RecordSize) > data.Length)
        {
            return [];
        }

        return Enumerable.Range(0, FremenTroopSlots)
            .TakeWhile(index => data[offset + (index * FremenTroop.RecordSize)] != 0)
            .Select(index => new FremenTroop(
                data.AsSpan(offset + (index * FremenTroop.RecordSize), FremenTroop.RecordSize)))
            .ToArray();
    }

    private static Dictionary<TroopId, DuneLocation> IndexTroopLocations(
        IReadOnlyList<DuneLocation> locations,
        IReadOnlyList<FremenTroop> troops)
    {
        var troopsById = troops.ToDictionary(troop => troop.Id);
        var index = new Dictionary<TroopId, DuneLocation>(troopsById.Count);

        foreach (var location in locations)
        {
            var troopId = location.PrimaryTroopId;
            var visited = new HashSet<TroopId>();
            while (troopId is { } id
                && visited.Add(id)
                && troopsById.TryGetValue(id, out var troop))
            {
                index.TryAdd(troop.Id, location);
                troopId = troop.NextTroopId;
            }
        }

        return index;
    }

    private static int FindLocationsOffset(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<LocationSignature> signatures)
    {
        var blockSize = ((signatures.Length - 1) * DuneLocation.RecordSize) + 3;
        if (data.Length < blockSize)
        {
            throw new InvalidDataException("The file is too short to contain Dune's location block.");
        }

        var lastStart = data.Length - blockSize;
        for (var candidate = 0; candidate <= lastStart; candidate++)
        {
            if (LocationBlockMatches(data, signatures, candidate))
            {
                return candidate;
            }
        }

        throw new InvalidDataException("The Dune location block could not be found in this file.");
    }

    private static bool LocationBlockMatches(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<LocationSignature> signatures,
        int candidate)
    {
        for (var index = 0; index < signatures.Length - 1; index++)
        {
            var signature = signatures[index];
            var offset = candidate + (index * DuneLocation.RecordSize);
            if (data[offset] != signature.RegionId || data[offset + 1] != signature.SubregionId)
            {
                return false;
            }
        }

        var terminator = signatures[^1];
        var terminatorOffset = candidate + ((signatures.Length - 1) * DuneLocation.RecordSize);
        return data[terminatorOffset] == terminator.RegionId
            && data[terminatorOffset + 1] == terminator.SubregionId
            && data[terminatorOffset + 2] == terminator.Terminator;
    }

    private void CopyLocationsTo(Span<byte> destination)
    {
        for (var index = 0; index < Locations.Count; index++)
        {
            var offset = locationsOffset + (index * DuneLocation.RecordSize);
            Locations[index].CopyTo(destination.Slice(offset, DuneLocation.RecordSize));
        }
    }

    private void CopyFremenTroopsTo(Span<byte> destination)
    {
        for (var index = 0; index < FremenTroops.Count; index++)
        {
            var offset = fremenTroopsOffset + (index * FremenTroop.RecordSize);
            FremenTroops[index].CopyTo(destination.Slice(offset, FremenTroop.RecordSize));
        }
    }
}
