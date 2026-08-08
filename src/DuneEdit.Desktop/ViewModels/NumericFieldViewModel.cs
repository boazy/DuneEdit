namespace DuneEdit.Desktop.ViewModels;

public sealed class NumericFieldViewModel(
    string label,
    Func<byte> read,
    Action<byte> write,
    Action? onChanged = null) : ViewModelBase
{
    public string Label { get; } = label;

    public decimal Value
    {
        get => read();
        set
        {
            var normalized = (byte)decimal.Clamp(decimal.Truncate(value), byte.MinValue, byte.MaxValue);
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
