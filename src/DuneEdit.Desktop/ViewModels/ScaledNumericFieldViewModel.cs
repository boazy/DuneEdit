namespace DuneEdit.Desktop.ViewModels;

public sealed class ScaledNumericFieldViewModel(
    string label,
    Func<int> read,
    Action<int> write,
    int maximum,
    int increment,
    Action? onChanged = null) : ViewModelBase
{
    public string Label { get; } = label;
    public int Maximum { get; } = maximum;
    public int Increment { get; } = increment;

    public decimal Value
    {
        get => read();
        set
        {
            var normalized = (int)decimal.Clamp(decimal.Round(value / Increment, 0, MidpointRounding.AwayFromZero) * Increment, 0, Maximum);
            if (read() == normalized)
            {
                return;
            }

            write(normalized);
            OnPropertyChanged();
            onChanged?.Invoke();
        }
    }
}
