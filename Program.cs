using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;

namespace WaterElectricityAutoClient;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        bool autoMode = args.Length > 0 && args[0] == "--auto";

        if (autoMode)
        {
            await RunAutoModeAsync();
        }
        else
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    static async Task RunAutoModeAsync()
    {
        var (emailSettings, querySettings, dengDengSettings) = LoadConfig();

        if (string.IsNullOrWhiteSpace(querySettings.Account))
        {
            Console.WriteLine("❌ 配置文件中的 Account 为空，请在 appsettings.json 中设置 QuerySettings.Account");
            return;
        }

        if (emailSettings.RecipientEmails.Count == 0 ||
            emailSettings.RecipientEmails.TrueForAll(string.IsNullOrWhiteSpace))
        {
            Console.WriteLine("❌ 配置文件中未设置收件人邮箱");
            return;
        }

        var emailService = new EmailService(emailSettings, querySettings.LowBalanceThreshold);
        DengDengPushService? pushService = null;
        if (dengDengSettings.Enabled && !string.IsNullOrWhiteSpace(dengDengSettings.BaseUrl) && !string.IsNullOrWhiteSpace(dengDengSettings.DeviceId))
        {
            pushService = new DengDengPushService(dengDengSettings);
        }

        do
        {
            Console.WriteLine($"\n⏳ [{DateTime.Now:HH:mm:ss}] 正在查询...");

            try
            {
                var rooms = await PerfectCampusApiClient.GetBoundRoomsAsync(querySettings.Account);

                if (rooms == null || rooms.Count == 0)
                {
                    Console.WriteLine("❌ 未找到任何绑定的房间");
                }
                else
                {
                    foreach (var r in rooms)
                    {
                        string icon = r.Status == 1 ? "🟢" : "🔴";
                        Console.WriteLine($"  {r.RoomName} | 余额:{r.Odd:F2}度 | 用量:{r.Use:F2}度 | {icon}");
                    }

                    await emailService.SendReportAsync(rooms);
                    Console.WriteLine($"📧 邮件已发送至 {string.Join(", ", emailSettings.RecipientEmails)}");

                    if (pushService != null)
                    {
                        await pushService.SendSummaryAsync(rooms);
                        Console.WriteLine("🔔 噔噔推送汇总已发送");

                        var lowBalanceRooms = rooms.FindAll(r => r.Odd < querySettings.LowBalanceThreshold);
                        if (lowBalanceRooms.Count > 0)
                        {
                            await pushService.SendLowBalanceAlertAsync(lowBalanceRooms, querySettings.LowBalanceThreshold);
                            Console.WriteLine("🔔 噔噔低余额预警已发送");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
            }

            if (querySettings.IntervalMinutes > 0)
            {
                Console.WriteLine($"⏰ 等待 {querySettings.IntervalMinutes} 分钟后进行下一次查询...");
                await Task.Delay(TimeSpan.FromMinutes(querySettings.IntervalMinutes));
            }

        } while (querySettings.IntervalMinutes > 0);
    }

    static (EmailSettings email, QuerySettings query, DengDengSettings dengDeng) LoadConfig()
    {
        return (
            SettingsService.LoadEmailSettings(),
            SettingsService.LoadQuerySettings(),
            SettingsService.LoadDengDengSettings()
        );
    }
}
