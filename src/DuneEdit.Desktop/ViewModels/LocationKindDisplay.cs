using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

internal static class LocationKindDisplay
{
    public static string ToDisplayTitle(this LocationKind kind) => kind switch
    {
        LocationKind.Sietch => "Sietch",
        LocationKind.Village => "Village",
        LocationKind.Fort => "Fort",
        LocationKind.CarthagPalace => "Carthag Palace",
        LocationKind.ArrakeenPalace => "Arrakeen Palace",
        LocationKind.Unknown => "Unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static string ToAssetName(this LocationKind kind) => kind switch
    {
        LocationKind.Sietch => "Sietch",
        LocationKind.Village => "Village",
        LocationKind.Fort => "Fort",
        LocationKind.CarthagPalace => "Carthag",
        LocationKind.ArrakeenPalace => "Arrakeen",
        LocationKind.Unknown => "Unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
