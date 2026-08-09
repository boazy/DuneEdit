using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

public sealed class FremenTroopMarkerViewModel : ViewModelBase
{
    private const double MapWidth = 1000;
    private const double MapHeight = 620;
    private const double MapMargin = 20;
    private const double SpriteScale = 0.105;

    private static readonly ConcurrentDictionary<string, Bitmap> Images = new(StringComparer.Ordinal);
    private bool isSelected;
    private bool isVisible = true;
    private readonly double centerX;
    private readonly double centerY;
    public FremenTroopMarkerViewModel(
        FremenTroop troop,
        Sietch location,
        Action<FremenTroopMarkerViewModel> select)
    {
        Troop = troop;
        Location = location;
        Image = LoadImage(troop);
        (Width, Height) = GetDimensions(Image);

        var (xOffset, yOffset) = GetLocationOffset(troop.PositionAroundLocation);
        centerX = ConvertCoordinate(location.MapPosX, MapWidth, byte.MaxValue) + xOffset;
        byte adjustedY = location.MapPosY > 180
            ? (byte)(location.MapPosY - 180)
            : (byte)(location.MapPosY + 75);
        centerY = ConvertCoordinate(adjustedY, MapHeight, 150) + yOffset;
        SelectCommand = new RelayCommand(() => select(this));
    }

    public FremenTroop Troop { get; }
    public Sietch Location { get; }
    public string Name => $"Fremen troop {Troop.Id:D2} — {Location.Name}";
    public Bitmap Image { get; private set; }
    public double Width { get; private set; }
    public double Height { get; private set; }
    public double Left => centerX - (Width / 2);
    public double Top => centerY - (Height / 2);
    public int ZIndex => IsSelected ? 3 : 2;
    public IRelayCommand SelectCommand { get; }

    public bool IsVisible
    {
        get => isVisible;
        set => SetProperty(ref isVisible, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (SetProperty(ref isSelected, value))
            {
                OnPropertyChanged(nameof(ZIndex));
            }
        }
    }

    public void RefreshDisplay()
    {
        var image = LoadImage(Troop);
        if (ReferenceEquals(Image, image))
        {
            return;
        }

        Image = image;
        (Width, Height) = GetDimensions(image);
        OnPropertyChanged(nameof(Image));
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
        OnPropertyChanged(nameof(Left));
        OnPropertyChanged(nameof(Top));
    }

    private static (double X, double Y) GetLocationOffset(byte position) => position switch
    {
        1 => (0, 13),
        2 => (11, 10),
        3 => (-11, 10),
        4 => (14, 0),
        5 => (-14, 0),
        6 => (11, -10),
        7 => (-11, -10),
        8 => (0, -13),
        _ => (0, 0),
    };

    private static Bitmap LoadImage(FremenTroop troop)
    {
        var asset = troop.IsRecruited
            ? troop.Role switch
            {
                FremenTroopRole.Spice => "FremenSpice.4xbrz.png",
                FremenTroopRole.Prospector => "FremenProspector.4xbrz.png",
                FremenTroopRole.Ecology => "FremenEcology.4xbrz.png",
                _ => "FremenFilled.4xbrz.png",
            }
            : "FremenOutline.4xbrz.png";

        return Images.GetOrAdd(asset, imageAsset =>
        {
            using var stream = AssetLoader.Open(new Uri($"avares://DuneEdit.Desktop/Assets/{imageAsset}"));
            return new Bitmap(stream);
        });
    }

    private static (double Width, double Height) GetDimensions(Bitmap image) =>
        (image.PixelSize.Width * SpriteScale, image.PixelSize.Height * SpriteScale);

    private static double ConvertCoordinate(byte coordinate, double maximum, byte coordinateMaximum)
    {
        var usable = maximum - (MapMargin * 2);
        return MapMargin + Math.Round((coordinate / (double)coordinateMaximum) * usable);
    }
}
