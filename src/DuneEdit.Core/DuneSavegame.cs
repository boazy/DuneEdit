namespace DuneEdit.Core;

public enum SavegameFormat
{
    CompressedSave,
    Executable,
}

public sealed class DuneSavegame
{
    private readonly byte[] decompressedData;
    private readonly int locationsOffset;
    private readonly Dictionary<(byte Region, byte Subregion), Sietch> locationsById;

    private DuneSavegame(
        byte[] decompressedData,
        SavegameFormat format,
        Loc[] locationSequences,
        int locationsOffset,
        string? sourcePath)
    {
        this.decompressedData = decompressedData;
        this.locationsOffset = locationsOffset;
        Format = format;
        SourcePath = sourcePath;

        var locations = new List<Sietch>(locationSequences.Length - 1);
        locationsById = new Dictionary<(byte Region, byte Subregion), Sietch>(locationSequences.Length - 1);

        for (var index = 0; index < locationSequences.Length - 1; index++)
        {
            var offset = locationsOffset + (index * Sietch.RecordSize);
            var location = new Sietch(decompressedData.AsSpan(offset, Sietch.RecordSize));
            locations.Add(location);
            locationsById.Add((location.RegionId, location.SubregionId), location);
        }

        Locations = locations.AsReadOnly();
    }

    public SavegameFormat Format { get; }
    public string? SourcePath { get; private set; }
    public IReadOnlyList<Sietch> Locations { get; }

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
        byte[] decompressed;
        Loc[] sequences;

        if (format == SavegameFormat.CompressedSave)
        {
            var declaredLength = SavegameCompression.ReadDeclaredFileLength(fileData);
            if (declaredLength != fileData.Length)
            {
                throw new InvalidDataException(
                    $"The save header declares {declaredLength} bytes, but the file contains {fileData.Length} bytes.");
            }

            decompressed = SavegameCompression.Decompress(fileData);
            sequences = LocSequences.compressed;
        }
        else
        {
            decompressed = fileData.ToArray();
            sequences = LocSequences.uncompressed;
        }

        var offset = FindLocationsOffset(decompressed, sequences);
        return new DuneSavegame(decompressed, format, sequences, offset, sourcePath);
    }

    public Sietch? FindLocation(byte region, byte subregion)
    {
        return locationsById.GetValueOrDefault((region, subregion));
    }

    public byte[] ToDecompressedBytes()
    {
        var output = (byte[])decompressedData.Clone();
        CopyLocationsTo(output);
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

    private static int FindLocationsOffset(ReadOnlySpan<byte> data, IReadOnlyList<Loc> sequences)
    {
        var blockSize = ((sequences.Count - 1) * Sietch.RecordSize) + 3;
        if (data.Length < blockSize)
        {
            throw new InvalidDataException("The file is too short to contain Dune's location block.");
        }

        var lastStart = data.Length - blockSize;
        for (var candidate = 0; candidate <= lastStart; candidate++)
        {
            var allMatch = true;
            for (var sequenceIndex = 0; sequenceIndex < sequences.Count - 1; sequenceIndex++)
            {
                var sequence = sequences[sequenceIndex];
                var offset = candidate + (sequenceIndex * Sietch.RecordSize);
                if (data[offset] != sequence.v1 || data[offset + 1] != sequence.v2)
                {
                    allMatch = false;
                    break;
                }
            }

            var terminator = sequences[^1];
            var terminatorOffset = candidate + ((sequences.Count - 1) * Sietch.RecordSize);
            allMatch = allMatch
                && data[terminatorOffset] == terminator.v1
                && data[terminatorOffset + 1] == terminator.v2
                && data[terminatorOffset + 2] == terminator.v3;

            if (allMatch)
            {
                return candidate;
            }
        }

        throw new InvalidDataException("The Dune location block could not be found in this file.");
    }

    private void CopyLocationsTo(Span<byte> destination)
    {
        for (var index = 0; index < Locations.Count; index++)
        {
            var offset = locationsOffset + (index * Sietch.RecordSize);
            Locations[index].CopyTo(destination.Slice(offset, Sietch.RecordSize));
        }
    }
}
