using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace WaterElectricityAutoClient;

public class EmailService
{
    private readonly EmailSettings _settings;
    private readonly double _lowBalanceThreshold;

    public EmailService(EmailSettings settings, double lowBalanceThreshold = 20.0)
    {
        _settings = settings;
        _lowBalanceThreshold = lowBalanceThreshold;
    }

    public async Task SendReportAsync(List<RoomInfo> rooms)
    {
        using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.SenderEmail, _settings.AuthCode)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = $"水电查询报告 - {DateTime.Now:yyyy-MM-dd HH:mm}",
            Body = BuildHtmlBody(rooms),
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8
        };

        foreach (var recipient in _settings.RecipientEmails)
        {
            if (!string.IsNullOrWhiteSpace(recipient))
                message.To.Add(recipient.Trim());
        }

        await client.SendMailAsync(message);
    }

    private string BuildHtmlBody(List<RoomInfo> rooms)
    {
        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset='utf-8'/>");
        sb.Append("<style>");
        sb.Append("body{font-family:'Microsoft YaHei',Arial,sans-serif;background:#f5f5f5;padding:20px;}");
        sb.Append(".container{max-width:600px;margin:0 auto;background:#fff;border-radius:8px;padding:24px;box-shadow:0 2px 8px rgba(0,0,0,0.1);}");
        sb.Append("h2{color:#333;border-bottom:2px solid #4CAF50;padding-bottom:10px;}");
        sb.Append("table{width:100%;border-collapse:collapse;margin:16px 0;}");
        sb.Append("th{background:#4CAF50;color:#fff;padding:10px;text-align:left;}");
        sb.Append("td{padding:10px;border-bottom:1px solid #eee;}");
        sb.Append("tr:hover{background:#f9f9f9;}");
        sb.Append(".warn{color:#f44336;font-weight:bold;}");
        sb.Append(".normal{color:#4CAF50;}");
        sb.Append(".offline{color:#f44336;}");
        sb.Append(".footer{margin-top:20px;color:#999;font-size:12px;text-align:center;}");
        sb.Append("</style></head><body>");
        sb.Append("<div class='container'>");
        sb.Append("<h2>🏠 完美校园水电查询报告</h2>");
        sb.Append($"<p>📅 查询时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.Append($"<p>📊 共查询到 <b>{rooms.Count}</b> 个房间</p>");

        sb.Append("<table>");
        sb.Append("<tr><th>房间名称</th><th>余额 (度)</th><th>用量 (度)</th><th>状态</th></tr>");

        foreach (var room in rooms)
        {
            string balanceClass = room.Odd < _lowBalanceThreshold ? "warn" : "normal";
            string statusText = room.Status == 1 ? "🟢 正常" : "🔴 异常";
            string statusClass = room.Status == 1 ? "normal" : "offline";

            sb.Append("<tr>");
            sb.Append($"<td>{room.RoomName}</td>");
            sb.Append($"<td class='{balanceClass}'>{room.Odd:F2}</td>");
            sb.Append($"<td>{room.Use:F2}</td>");
            sb.Append($"<td class='{statusClass}'>{statusText}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</table>");

        foreach (var room in rooms)
        {
            if (room.Odd < _lowBalanceThreshold)
            {
                sb.Append($"<p class='warn'>⚠️ 警告：房间 <b>{room.RoomName}</b> 余额仅剩 {room.Odd:F2} 度，低于阈值 {_lowBalanceThreshold} 度，请及时充值！</p>");
            }
        }

        sb.Append("<p class='footer'>此邮件由水电查询助手自动发送</p>");
        sb.Append("</div></body></html>");

        return sb.ToString();
    }
}
