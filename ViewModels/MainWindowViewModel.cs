using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WaterElectricityAutoClient.Views;

namespace WaterElectricityAutoClient.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private double _sidebarWidth = 200;

    [ObservableProperty]
    private int _selectedNavIndex = 0;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private bool _isDarkTheme;

    private readonly QueryViewModel _queryViewModel = new();
    private readonly PushSettingsViewModel _pushSettingsViewModel = new();
    private readonly QueryView _queryView;
    private readonly PushSettingsView _pushSettingsView;

    public MainWindowViewModel()
    {
        _queryView = new QueryView { DataContext = _queryViewModel };
        _pushSettingsView = new PushSettingsView { DataContext = _pushSettingsViewModel };
        CurrentView = _queryView;
        IsDarkTheme = SettingsService.LoadTheme() == "Dark";

        // Sync selected room to push settings
        _queryViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(QueryViewModel.SelectedRoom))
                _pushSettingsViewModel.CurrentRoom = _queryViewModel.SelectedRoom;
        };
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
        SidebarWidth = IsSidebarExpanded ? 200 : 52;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        var theme = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = theme;
        SettingsService.SaveTheme(IsDarkTheme ? "Dark" : "Light");
    }

    partial void OnSelectedNavIndexChanged(int value)
    {
        CurrentView = value switch
        {
            0 => _queryView,
            1 => _pushSettingsView,
            _ => CurrentView
        };
    }
}
