using DuneEdit.Core;

if (args is not [var mapPath, var latitudeTablePath, var paletteResourcePath, var outputPath])
{
    Console.Error.WriteLine("Usage: TerrainMap <MAP.HSQ> <TABLAT.BIN> <ONMAP.HSQ> <output.png>");
    return 2;
}

try
{
    var compressedMap = File.ReadAllBytes(mapPath);
    var latitudeTable = File.ReadAllBytes(latitudeTablePath);
    var compressedPaletteResource = File.ReadAllBytes(paletteResourcePath);
    var pixels = TerrainMapDecoder.DecodeRgba32(compressedMap, latitudeTable, compressedPaletteResource);
    PngWriter.WriteRgba32(outputPath, TerrainMapDecoder.Width, TerrainMapDecoder.Height, pixels);
    Console.WriteLine($"Wrote {TerrainMapDecoder.Width}x{TerrainMapDecoder.Height} terrain map to {outputPath}.");
    return 0;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
{
    Console.Error.WriteLine($"TerrainMap: {error.Message}");
    return 1;
}
