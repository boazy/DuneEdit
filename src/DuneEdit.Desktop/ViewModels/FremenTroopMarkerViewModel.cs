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
        DuneLocation location,
        Action<FremenTroopMarkerViewModel> select)
    {
        Troop = troop;
        Location = location;
        Image = LoadImage(troop);
        (Width, Height) = GetDimensions(Image);

        var center = MapProjection.Project(location.MapPosition, MapWidth, MapHeight, MapMargin);
        var offset = MapProjection.GetTroopOffset(troop.Placement);
        centerX = center.X + offset.X;
        centerY = center.Y + offset.Y;
        SelectCommand = new RelayCommand(() => select(this));
    }

    public FremenTroop Troop { get; }
    public DuneLocation Location { get; }
    public string Name => $"Fremen troop {Troop.Id.Value:D2} — {Location.Name}";
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

}
