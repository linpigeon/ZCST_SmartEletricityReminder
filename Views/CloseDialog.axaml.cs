using Avalonia.Controls;

namespace WaterElectricityAutoClient.Views;

public partial class CloseDialog : Window
{
    public enum CloseChoice { Background, Exit }

    public CloseChoice Choice { get; private set; } = CloseChoice.Exit;

    public CloseDialog()
    {
        InitializeComponent();
    }

    private void OnBackgroundClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Choice = CloseChoice.Background;
        Close();
    }

    private void OnExitClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Choice = CloseChoice.Exit;
        Close();
    }
}
