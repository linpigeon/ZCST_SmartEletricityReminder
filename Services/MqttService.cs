using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;

namespace WaterElectricityAutoClient;

public class MqttService
{
    private const int KeepAliveSeconds = 20;
    private const int TimeoutMs = 10000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly MqttSettings _settings;
    private readonly double _lowBalanceThreshold;

    public MqttService(MqttSettings settings, double lowBalanceThreshold = 20.0)
    {
        _settings = settings;
        _lowBalanceThreshold = lowBalanceThreshold;
    }

    public async Task<bool> SendReportAsync(List<RoomInfo> rooms)
    {
        var payload = new
        {
            type = "electricity_report",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            rooms = rooms.Select(r => new
            {
                name = r.RoomName,
                balance = Math.Round(r.Odd, 2),
                usage = Math.Round(r.Use, 2),
                status = r.Status == 1 ? "normal" : "abnormal"
            }),
            lowBalanceAlerts = rooms
                .Where(r => r.Odd < _lowBalanceThreshold)
                .Select(r => new
                {
                    name = r.RoomName,
                    balance = Math.Round(r.Odd, 2)
                })
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return await PublishAsync(json);
    }

    public async Task<bool> SendLowBalanceAlertAsync(List<RoomInfo> lowBalanceRooms, double threshold)
    {
        if (lowBalanceRooms.Count == 0) return true;

        var payload = new
        {
            type = "low_balance_alert",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            threshold,
            rooms = lowBalanceRooms.Select(r => new
            {
                name = r.RoomName,
                balance = Math.Round(r.Odd, 2)
            })
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return await PublishAsync(json);
    }

    // 对应 C 代码 main() 的完整流程:
    //   MQTTClient_create → connect → publish → waitForCompletion → disconnect → destroy
    // 异常不在此层捕获，由上层 ViewModel 的 try/catch 统一处理并展示到 UI
    private async Task<bool> PublishAsync(string payload)
    {
        // MQTTClient_create(&client, ADDRESS, CLIENTID, MQTTCLIENT_PERSISTENCE_NONE, NULL)
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        var brokerAddress = _settings.BrokerAddress.Trim();
        if (brokerAddress.StartsWith("mqtt://", StringComparison.OrdinalIgnoreCase))
            brokerAddress = brokerAddress[7..];
        else if (brokerAddress.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
            brokerAddress = brokerAddress[6..];

        var clientId = $"elec-rem-{Guid.NewGuid():N}"[..23];

        // conn_opts.keepAliveInterval = 20
        // conn_opts.cleansession = 1
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(brokerAddress, _settings.Port)
            .WithClientId(clientId)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(KeepAliveSeconds))
            .WithCleanSession()
            .WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

        if (!string.IsNullOrWhiteSpace(_settings.Username))
            builder = builder.WithCredentials(_settings.Username, _settings.Password);

        var options = builder.Build();

        // MQTTClient_connect(client, &conn_opts)
        var connectResult = await client.ConnectAsync(options);
        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            // 用 throw 而非 return false，错误信息才能到达 ViewModel 的 catch 块显示在 UI
            throw new InvalidOperationException(
                $"连接Broker失败 ({connectResult.ResultCode})");
        }

        // pubmsg.payload = PAYLOAD
        // pubmsg.payloadlen = strlen(PAYLOAD)
        // pubmsg.qos = QOS          (= 1)
        // pubmsg.retained = 0
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(_settings.Topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        // MQTTClient_publishMessage(client, TOPIC, &pubmsg, &token)
        // MQTTClient_waitForCompletion(client, token, TIMEOUT)
        var publishResult = await client.PublishAsync(message);
        // MQTT 5.0: 0x00=Success, 0x10=NoMatchingSubscribers(消息已被Broker接收但暂无订阅者)
        // NoMatchingSubscribers 是正常情况(如ESP32未在线)，不视为失败
        if (publishResult.ReasonCode is not MqttClientPublishReasonCode.Success
            and not MqttClientPublishReasonCode.NoMatchingSubscribers)
        {
            throw new InvalidOperationException(
                $"发布失败 ({publishResult.ReasonCode})");
        }

        // MQTTClient_disconnect(client, 10000)
        await client.DisconnectAsync();

        // MQTTClient_destroy(&client) — handled by using
        return true;
    }
}
