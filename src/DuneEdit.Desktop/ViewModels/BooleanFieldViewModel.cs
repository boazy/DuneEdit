namespace DuneEdit.Desktop.ViewModels;

public sealed class BooleanFieldViewModel(
    string label,
    Func<bool> read,
    Action<bool> write,
    Action? onChanged = null) : ViewModelBase
{
    public string Label { get; } = label;

    public bool Value
    {
        get => read();
        set
        {
            if (read() == value)
            {
                return;
            }

            write(value);
            OnPropertyChanged();
            onChanged?.Invoke();
        }
    }
}
