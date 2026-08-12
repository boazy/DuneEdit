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
    private const double SpriteUpscaleFactor = 4;

    private static readonly ConcurrentDictionary<LocationKind, Bitmap> Images = new();
    private bool isSelected;
    private bool isVisible = true;
    private readonly double centerX;
    private readonly double centerY;

    public LocationMarkerViewModel(DuneLocation location, Action<LocationMarkerViewModel> select)
    {
        Location = location;
        Image = Images.GetOrAdd(location.Kind, LoadImage);
        var imageScale = (location.Kind == LocationKind.Sietch ? 0.35 : 0.27) / SpriteUpscaleFactor;
        Width = Image.PixelSize.Width * imageScale;
        Height = Image.PixelSize.Height * imageScale;
        var center = MapProjection.Project(location.MapPosition, MapWidth, MapHeight, MapMargin);
        centerX = center.X;
        centerY = center.Y;
        SelectCommand = new RelayCommand(() => select(this));
    }

    public DuneLocation Location { get; }
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


    private static Bitmap LoadImage(LocationKind kind)
    {
        var uri = new Uri($"avares://DuneEdit.Desktop/Assets/Locations/{kind.ToAssetName()}.4xbrz.png");
        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }
}
