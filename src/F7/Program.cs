using DuneEdit.Core;

return Run(args);

static int Run(string[] arguments)
{
    if (arguments is not [var operation, var filePath]
        || operation is not ("-c" or "-d"))
    {
        PrintUsage();
        return 2;
    }

    try
    {
        var input = File.ReadAllBytes(filePath);
        var output = operation == "-c"
            ? SavegameCompression.Compress(input)
            : SavegameCompression.Decompress(input);

        WriteAtomically(filePath, output);
        return 0;
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
    {
        Console.Error.WriteLine($"f7: {error.Message}");
        return 1;
    }
}

static void WriteAtomically(string filePath, byte[] data)
{
    var fullPath = Path.GetFullPath(filePath);
    var directory = Path.GetDirectoryName(fullPath)
        ?? throw new InvalidOperationException("The file path has no parent directory.");
    var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

    try
    {
        File.WriteAllBytes(temporaryPath, data);
        File.Move(temporaryPath, fullPath, overwrite: true);
    }
    finally
    {
        File.Delete(temporaryPath);
    }
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  f7 -c <file>    Compress a Dune save in place");
    Console.Error.WriteLine("  f7 -d <file>    Decompress a Dune save in place");
}
