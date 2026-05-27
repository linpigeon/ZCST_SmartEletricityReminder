namespace WaterElectricityAutoClient;

public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.qq.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = "";
    public string SenderName { get; set; } = "水电查询助手";
    public string AuthCode { get; set; } = "";
    public List<string> RecipientEmails { get; set; } = new();
    public bool EnableSsl { get; set; } = true;
}

public class QuerySettings
{
    public string Account { get; set; } = "";
    public int IntervalMinutes { get; set; } = 0;
    public double LowBalanceThreshold { get; set; } = 20.0;
}

public class DengDengSettings
{
    public string BaseUrl { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public bool Enabled { get; set; } = false;
}
