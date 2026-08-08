using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

public sealed class LocationMarkerViewModel : ViewModelBase
{
    private const double MapWidth = 1000;
    private const double MapHeight = 620;
    private const double MapMargin = 20;

    private static readonly ConcurrentDictionary<string, Bitmap> Images = new(StringComparer.Ordinal);
    private bool isSelected;
    private bool isVisible = true;
    private readonly double centerX;
    private readonly double centerY;

    public LocationMarkerViewModel(Sietch location, Action<LocationMarkerViewModel> select)
    {
        Location = location;
        Image = Images.GetOrAdd(location.LocationTypeGroup, LoadImage);
        var imageScale = location.LocationTypeGroup == "Sietch" ? 0.35 : 0.27;
        Width = Image.PixelSize.Width * imageScale;
        Height = Image.PixelSize.Height * imageScale;
        centerX = ConvertCoordinate(location.MapPosX, MapWidth, byte.MaxValue);

        byte adjustedY = location.MapPosY > 180
            ? (byte)(location.MapPosY - 180)
            : (byte)(location.MapPosY + 75);
        centerY = ConvertCoordinate(adjustedY, MapHeight, 150);
        SelectCommand = new RelayCommand(() => select(this));
    }

    public Sietch Location { get; }
    public string Name => Location.Name;
    public Bitmap Image { get; }
    public double Width { get; }
    public double Height { get; }
    public double DisplayWidth => Width * SelectionScale;
    public double DisplayHeight => Height * SelectionScale;
    public double Left => centerX - (DisplayWidth / 2);
    public double Top => centerY - (DisplayHeight / 2);
    public int ZIndex => IsSelected ? 1 : 0;
    public IRelayCommand SelectCommand { get; }
    public double SelectionScale => IsSelected ? 1.60 : 1.0;

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
            if (!SetProperty(ref isSelected, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectionScale));
            OnPropertyChanged(nameof(DisplayWidth));
            OnPropertyChanged(nameof(DisplayHeight));
            OnPropertyChanged(nameof(Left));
            OnPropertyChanged(nameof(Top));
            OnPropertyChanged(nameof(ZIndex));
        }
    }

    private static double ConvertCoordinate(byte coordinate, double maximum, byte coordinateMaximum)
    {
        var usable = maximum - (MapMargin * 2);
        return MapMargin + Math.Round((coordinate / (double)coordinateMaximum) * usable);
    }

    private static Bitmap LoadImage(string locationType)
    {
        var uri = new Uri($"avares://DuneEdit.Desktop/Assets/Locations/{locationType}.png");
        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }
}
