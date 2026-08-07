using Avalonia.Interactivity;

namespace DuneEdit.Desktop.Views;

public partial class MessageDialog : Avalonia.Controls.Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    public MessageDialog(string title, string message)
        : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void CloseClicked(object? sender, RoutedEventArgs eventArgs)
    {
        Close();
    }
}
