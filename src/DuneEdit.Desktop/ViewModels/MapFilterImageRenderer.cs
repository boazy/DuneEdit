using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

internal static class MapFilterImageRenderer
{
    private static readonly Color Atreides = Color.FromArgb(205, 201, 67, 57);
    private static readonly Color Harkonnen = Color.FromArgb(205, 47, 84, 157);
    private static readonly Color Undiscovered = Color.FromArgb(232, 18, 20, 20);
    private static readonly Color LowSpice = Color.FromArgb(214, 130, 63, 22);
    private static readonly Color HighSpice = Color.FromArgb(224, 255, 210, 76);

    public static WriteableBitmap Render(IReadOnlyList<Sietch> locations, MapFilter filter)
    {
        var locationsByField = new Sietch?[byte.MaxValue + 1];
        foreach (var location in locations)
        {
            locationsByField[location.SpiceFieldId] = location;
        }

        var palette = new Color[byte.MaxValue + 1];
        for (var field = 0; field < palette.Length; field++)
        {
            palette[field] = GetColor(locationsByField[field], filter);
        }

        var cells = MapZones.Cells;
        var pixels = GC.AllocateUninitializedArray<int>(cells.Length);
        for (var index = 0; index < cells.Length; index++)
        {
            pixels[index] = (int)palette[cells[index]].ToUInt32();
        }

        var bitmap = new WriteableBitmap(
            new PixelSize(MapZones.Width, MapZones.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using var framebuffer = bitmap.Lock();
        Marshal.Copy(pixels, 0, framebuffer.Address, pixels.Length);
        return bitmap;
    }

    private static Color GetColor(Sietch? location, MapFilter filter) => filter switch
    {
        MapFilter.AreaControl => location?.Controller switch
        {
            AreaController.Atreides => Atreides,
            AreaController.Harkonnen => Harkonnen,
            _ => Colors.Transparent,
        },
        MapFilter.SpiceDensity => location is null || location.SpiceDensity == 0
            ? Colors.Transparent
            : InterpolateSpice(location.SpiceDensity),
        MapFilter.Discovery => location?.Discovered == true
            ? Colors.Transparent
            : Undiscovered,
        _ => Colors.Transparent,
    };

    private static Color InterpolateSpice(byte density)
    {
        var amount = Math.Sqrt(density / (double)byte.MaxValue);
        return Color.FromArgb(
            (byte)Math.Round(LowSpice.A + ((HighSpice.A - LowSpice.A) * amount)),
            (byte)Math.Round(LowSpice.R + ((HighSpice.R - LowSpice.R) * amount)),
            (byte)Math.Round(LowSpice.G + ((HighSpice.G - LowSpice.G) * amount)),
            (byte)Math.Round(LowSpice.B + ((HighSpice.B - LowSpice.B) * amount)));
    }
}
