using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WaterElectricityAutoClient.ViewModels;

public partial class PushSettingsViewModel : ObservableObject
{
    // Email settings
    [ObservableProperty] private string _smtpServer = "smtp.qq.com";
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string _senderEmail = "";
    [ObservableProperty] private string _senderName = "水电查询助手";
    [ObservableProperty] private string _authCode = "";
    [ObservableProperty] private string _recipientEmails = "";
    [ObservableProperty] private bool _enableSsl = true;

    // DengDeng settings
    [ObservableProperty] private bool _dengDengEnabled;
    [ObservableProperty] private string _dengDengBaseUrl = "";
    [ObservableProperty] private string _dengDengDeviceId = "";

    // Shared state
    [ObservableProperty] private double _lowBalanceThreshold = 20.0;
    [ObservableProperty] private int _intervalMinutes = 30;
    [ObservableProperty] private string _account = "";

    // Current room (set from query page)
    [ObservableProperty] private RoomInfo? _currentRoom;

    // Auto-push state
    [ObservableProperty] private bool _isAutoPushRunning;
    [ObservableProperty] private string _autoPushStatus = "";

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isTesting;

    private CancellationTokenSource? _autoPushCts;

    public PushSettingsViewModel()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        var email = SettingsService.LoadEmailSettings();
        SmtpServer = email.SmtpServer;
        SmtpPort = email.SmtpPort;
        SenderEmail = email.SenderEmail;
        SenderName = email.SenderName;
        AuthCode = email.AuthCode;
        RecipientEmails = string.Join("; ", email.RecipientEmails.Where(e => !string.IsNullOrWhiteSpace(e)));
        EnableSsl = email.EnableSsl;

        var dengDeng = SettingsService.LoadDengDengSettings();
        DengDengEnabled = dengDeng.Enabled;
        DengDengBaseUrl = dengDeng.BaseUrl;
        DengDengDeviceId = dengDeng.DeviceId;

        var query = SettingsService.LoadQuerySettings();
        LowBalanceThreshold = query.LowBalanceThreshold;
        IntervalMinutes = query.IntervalMinutes > 0 ? query.IntervalMinutes : 30;
        Account = query.Account;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var email = new EmailSettings
        {
            SmtpServer = SmtpServer,
            SmtpPort = SmtpPort,
            SenderEmail = SenderEmail,
            SenderName = SenderName,
            AuthCode = AuthCode,
            RecipientEmails = RecipientEmails.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList(),
            EnableSsl = EnableSsl
        };

        var dengDeng = new DengDengSettings
        {
            Enabled = DengDengEnabled,
            BaseUrl = DengDengBaseUrl,
            DeviceId = DengDengDeviceId
        };

        var query = SettingsService.LoadQuerySettings();
        query.LowBalanceThreshold = LowBalanceThreshold;
        query.IntervalMinutes = IntervalMinutes;
        query.Account = Account;

        SettingsService.SaveAllSettings(email, query, dengDeng);
        StatusMessage = "设置已保存";
    }

    [RelayCommand]
    private async Task TestPushAsync()
    {
        IsTesting = true;
        StatusMessage = "正在测试推送...";

        var rooms = GetTargetRooms();
        var results = await DoPushAsync(rooms);

        StatusMessage = string.Join(" | ", results);
        IsTesting = false;
    }

    [RelayCommand]
    private async Task StartAutoPushAsync()
    {
        // Always read latest account from settings
        var querySettings = SettingsService.LoadQuerySettings();
        Account = querySettings.Account;

        if (string.IsNullOrWhiteSpace(Account))
        {
            StatusMessage = "请先在查询页面输入学号并点击保存";
            return;
        }

        IsAutoPushRunning = true;
        _autoPushCts = new CancellationTokenSource();
        AutoPushStatus = "运行中";
        StatusMessage = $"自动推送已启动 (间隔 {IntervalMinutes} 分钟)";

        try
        {
            while (!_autoPushCts.Token.IsCancellationRequested)
            {
                var delayMs = IntervalMinutes * 60 * 1000;
                await Task.Delay(delayMs, _autoPushCts.Token);

                var now = DateTime.Now;
                AutoPushStatus = $"上次推送: {now:HH:mm:ss}";

                List<RoomInfo>? rooms;
                try
                {
                    rooms = await PerfectCampusApiClient.GetBoundRoomsAsync(Account);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"自动推送 [{now:HH:mm}]: 查询失败 - {ex.Message}";
                    continue;
                }

                if (rooms == null || rooms.Count == 0)
                {
                    StatusMessage = $"自动推送 [{now:HH:mm}]: 未查询到房间数据";
                    continue;
                }

                var targetRooms = CurrentRoom != null
                    ? rooms.Where(r => r.RoomVerify == CurrentRoom.RoomVerify).ToList()
                    : rooms;

                if (targetRooms.Count == 0)
                {
                    StatusMessage = $"自动推送 [{now:HH:mm}]: 未找到选中的房间";
                    continue;
                }

                var results = await DoPushAsync(targetRooms);
                StatusMessage = $"自动推送 [{now:HH:mm}]: {string.Join(", ", results)}";
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            StatusMessage = $"自动推送异常: {ex.Message}";
        }
        finally
        {
            IsAutoPushRunning = false;
            AutoPushStatus = "已停止";
        }
    }

    [RelayCommand]
    private void StopAutoPush()
    {
        _autoPushCts?.Cancel();
        IsAutoPushRunning = false;
        AutoPushStatus = "已停止";
        StatusMessage = "自动推送已停止";
    }

    private List<RoomInfo> GetTargetRooms()
    {
        if (CurrentRoom != null)
            return new List<RoomInfo> { CurrentRoom };

        return new List<RoomInfo>
        {
            new RoomInfo { RoomName = "测试房间", RoomVerify = "TEST", Odd = 100.0, Use = 50.0, Status = 1 }
        };
    }

    private async Task<List<string>> DoPushAsync(List<RoomInfo> rooms)
    {
        var email = new EmailSettings
        {
            SmtpServer = SmtpServer,
            SmtpPort = SmtpPort,
            SenderEmail = SenderEmail,
            SenderName = SenderName,
            AuthCode = AuthCode,
            RecipientEmails = RecipientEmails.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList(),
            EnableSsl = EnableSsl
        };

        var dengDeng = new DengDengSettings
        {
            Enabled = DengDengEnabled,
            BaseUrl = DengDengBaseUrl,
            DeviceId = DengDengDeviceId
        };

        var results = new List<string>();

        // Email
        try
        {
            if (string.IsNullOrWhiteSpace(email.SenderEmail) || string.IsNullOrWhiteSpace(email.AuthCode))
            {
                results.Add("邮件: 配置不完整");
            }
            else if (email.RecipientEmails.Count == 0 || email.RecipientEmails.TrueForAll(string.IsNullOrWhiteSpace))
            {
                results.Add("邮件: 未设置收件人");
            }
            else
            {
                var emailService = new EmailService(email, LowBalanceThreshold);
                await emailService.SendReportAsync(rooms);
                results.Add("邮件已发送");
            }
        }
        catch (Exception ex)
        {
            results.Add($"邮件失败: {ex.Message}");
        }

        // DengDeng
        if (dengDeng.Enabled)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dengDeng.BaseUrl) || string.IsNullOrWhiteSpace(dengDeng.DeviceId))
                {
                    results.Add("噔噔: 配置不完整");
                }
                else
                {
                    var pushService = new DengDengPushService(dengDeng);
                    var ok = await pushService.SendSummaryAsync(rooms);
                    results.Add(ok ? "噔噔已发送" : "噔噔失败");

                    var lowBalance = rooms.Where(r => r.Odd < LowBalanceThreshold).ToList();
                    if (lowBalance.Count > 0)
                    {
                        await pushService.SendLowBalanceAlertAsync(lowBalance, LowBalanceThreshold);
                        results.Add("低余额预警已发送");
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add($"噔噔失败: {ex.Message}");
            }
        }

        return results;
    }
}
