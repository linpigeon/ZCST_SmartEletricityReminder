using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WaterElectricityAutoClient;

public class DengDengPushService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _deviceId;

    public DengDengPushService(DengDengSettings settings)
    {
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _deviceId = settings.DeviceId;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<bool> SendSummaryAsync(List<RoomInfo> rooms)
    {
        int normalCount = rooms.FindAll(r => r.Status == 1).Count;
        int offlineCount = rooms.Count - normalCount;

        var lines = new List<string>();
        foreach (var r in rooms)
        {
            string status = r.Status == 1 ? "正常" : "异常";
            lines.Add($"{r.RoomName} 余额{r.Odd:F1}度 {status}");
        }

        string title = $"水电查询报告 ({rooms.Count}个房间)";
        string content = string.Join("；", lines);

        if (offlineCount > 0)
            content += $" | ⚠️ {offlineCount}个设备离线";

        return await PushAsync(title, content);
    }

    public async Task<bool> SendLowBalanceAlertAsync(List<RoomInfo> lowBalanceRooms, double threshold)
    {
        if (lowBalanceRooms.Count == 0) return true;

        var lines = new List<string>();
        foreach (var r in lowBalanceRooms)
            lines.Add($"{r.RoomName} 仅剩{r.Odd:F1}度");

        string title = $"⚠️ 电费余额不足预警";
        string content = $"以下房间余额低于{threshold}度：{string.Join("；", lines)}，请及时充值！";

        return await PushAsync(title, content);
    }

    private async Task<bool> PushAsync(string title, string content)
    {
        try
        {
            string url = $"{_baseUrl}/api/v1/push/notification" +
                $"?device_id={Uri.EscapeDataString(_deviceId)}" +
                $"&title={Uri.EscapeDataString(title)}" +
                $"&content={Uri.EscapeDataString(content)}";

            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  🔔 噔噔推送失败: {ex.Message}");
            return false;
        }
    }
}
