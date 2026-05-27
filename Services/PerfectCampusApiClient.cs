using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WaterElectricityAutoClient;

public static class PerfectCampusApiClient
{
    private static readonly string Url = "https://xqh5.17wanxiao.com/smartWaterAndElectricityService/SWAEServlet";

    private static readonly Dictionary<string, string> InitialCookies = new()
    {
        { "acw_tc", "0a45645e17625760507803047ec74cd7857cf8506787198497f848f5ac54ba" },
        { "SERVERID", "7abd666da76fadf6bd7f0a8acd3e2ff1|1725968099|1725968080" }
    };

    public static async Task<List<RoomInfo>?> GetBoundRoomsAsync(string account)
    {
        var handler = CreateHttpClientHandler();
        using (var httpClient = new HttpClient(handler))
        {
            SetupHeaders(httpClient, isMobile: true);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

            var paramObj = new
            {
                cmd = "getbindroom",
                account = account,
                timestamp = timestamp
            };
            string paramJson = JsonSerializer.Serialize(paramObj);

            var formData = new Dictionary<string, string>
            {
                { "param", paramJson },
                { "customercode", "1399" },
                { "method", "getbindroom" },
                { "command", "JBSWaterElecService" }
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await httpClient.PostAsync(Url, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ 网络请求失败: {response.StatusCode}");
                return null;
            }

            string responseBody = await response.Content.ReadAsStringAsync();

            return JsonResponseParser.ParseRoomListWithDetails(responseBody);
        }
    }

    private static HttpClientHandler CreateHttpClientHandler()
    {
        var cookieContainer = new CookieContainer();
        var uri = new Uri(Url);
        foreach (var kvp in InitialCookies)
        {
            try
            {
                cookieContainer.Add(uri, new Cookie(kvp.Key, kvp.Value));
            }
            catch { }
        }
        return new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }

    private static void SetupHeaders(HttpClient client, bool isMobile)
    {
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        client.DefaultRequestHeaders.Referrer = new Uri("https://xqh5.17wanxiao.com/userwaterelecmini/index.html");

        if (isMobile)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Linux; Android 14; REP-AN00 Build/HONORREP-AN00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/127.0.6533.103 Mobile Safari/537.36 Wanxiao/5.8.6");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "com.newcapec.mobile.ncp");
            client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", "?1");
            client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", "\"Android\"");
        }
        else
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }
    }
}
