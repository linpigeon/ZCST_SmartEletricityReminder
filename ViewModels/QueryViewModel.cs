using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WaterElectricityAutoClient.ViewModels;

public partial class QueryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _account = "";

    [ObservableProperty]
    private ObservableCollection<RoomInfo> _rooms = new();

    [ObservableProperty]
    private RoomInfo? _selectedRoom;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    public QueryViewModel()
    {
        var settings = SettingsService.LoadQuerySettings();
        Account = settings.Account;
    }

    [RelayCommand]
    private async Task QueryRoomsAsync()
    {
        if (string.IsNullOrWhiteSpace(Account))
        {
            StatusMessage = "请先输入学号";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在查询...";
        Rooms.Clear();
        SelectedRoom = null;

        try
        {
            var rooms = await PerfectCampusApiClient.GetBoundRoomsAsync(Account);
            if (rooms == null || rooms.Count == 0)
            {
                StatusMessage = "未找到任何绑定的房间";
                return;
            }

            foreach (var r in rooms)
                Rooms.Add(r);

            StatusMessage = $"共查询到 {rooms.Count} 个房间";

            if (Rooms.Count == 1)
                SelectedRoom = Rooms[0];
        }
        catch (Exception ex)
        {
            StatusMessage = $"查询失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SaveAccount()
    {
        if (string.IsNullOrWhiteSpace(Account))
        {
            StatusMessage = "学号不能为空";
            return;
        }

        var settings = new QuerySettings { Account = Account };
        var existing = SettingsService.LoadQuerySettings();
        settings.IntervalMinutes = existing.IntervalMinutes;
        settings.LowBalanceThreshold = existing.LowBalanceThreshold;
        SettingsService.SaveQuerySettings(settings);
        StatusMessage = "学号已保存";
    }

    partial void OnSelectedRoomChanged(RoomInfo? value)
    {
        if (value != null)
            StatusMessage = $"已选择: {value.RoomName}";
    }
}
