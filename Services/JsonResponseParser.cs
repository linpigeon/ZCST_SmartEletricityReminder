using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WaterElectricityAutoClient;

public static class JsonResponseParser
{
    /// <summary>
    /// 核心解析逻辑：从 getbindroom 响应中提取所有数据
    /// 结构：root -> body(string) -> innerRoot -> roomlist -> detaillist -> [odd, use, status]
    /// </summary>
    public static List<RoomInfo>? ParseRoomListWithDetails(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            if (root.TryGetProperty("code_", out var codeEl))
            {
                int code = codeEl.ValueKind == JsonValueKind.String
                    ? (int.TryParse(codeEl.GetString(), out var c) ? c : -1)
                    : codeEl.GetInt32();

                if (code != 0)
                {
                    Console.WriteLine($"⚠️ 接口返回错误码: {code}");
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (!root.TryGetProperty("body", out var bodyEl) || bodyEl.ValueKind != JsonValueKind.String)
            {
                Console.WriteLine("⚠️ 未找到 body 字段或格式错误");
                return null;
            }

            string innerJson = bodyEl.GetString()!;
            using var innerDoc = JsonDocument.Parse(innerJson);
            var innerRoot = innerDoc.RootElement;

            var resultList = new List<RoomInfo>();

            if (innerRoot.TryGetProperty("roomlist", out var roomListEl) && roomListEl.ValueKind == JsonValueKind.Array && roomListEl.GetArrayLength() > 0)
            {
                foreach (var room in roomListEl.EnumerateArray())
                {
                    string fullName = room.TryGetProperty("roomfullname", out var fn) ? fn.GetString()! : "未知房间";
                    string verify = room.TryGetProperty("roomverify", out var rv) ? rv.GetString()! : "";

                    if (string.IsNullOrEmpty(verify)) continue;

                    double oddVal = 0.0;
                    double useVal = 0.0;
                    int statusVal = 0;

                    if (room.TryGetProperty("detaillist", out var detailList) && detailList.ValueKind == JsonValueKind.Array && detailList.GetArrayLength() > 0)
                    {
                        var detail = detailList[0];

                        if (detail.TryGetProperty("odd", out var oEl))
                        {
                            oddVal = oEl.ValueKind == JsonValueKind.String
                                ? (double.TryParse(oEl.GetString(), out var d) ? d : 0)
                                : (oEl.ValueKind == JsonValueKind.Number ? oEl.GetDouble() : 0);
                        }

                        if (detail.TryGetProperty("use", out var uEl))
                        {
                            useVal = uEl.ValueKind == JsonValueKind.String
                                ? (double.TryParse(uEl.GetString(), out var d) ? d : 0)
                                : (uEl.ValueKind == JsonValueKind.Number ? uEl.GetDouble() : 0);
                        }

                        if (detail.TryGetProperty("status", out var sEl))
                        {
                            if (sEl.ValueKind == JsonValueKind.String)
                            {
                                string sStr = sEl.GetString()!;
                                int.TryParse(sStr.Trim(), out statusVal);
                            }
                            else if (sEl.ValueKind == JsonValueKind.Number)
                            {
                                statusVal = sEl.GetInt32();
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ 房间 {fullName} 没有 detallist 数据，将显示默认值。");
                    }

                    resultList.Add(new RoomInfo
                    {
                        RoomName = fullName,
                        RoomVerify = verify,
                        Odd = oddVal,
                        Use = useVal,
                        Status = statusVal
                    });
                }

                return resultList;
            }

            if (innerRoot.TryGetProperty("roomfullname", out var topFn) && innerRoot.TryGetProperty("roomverify", out var topRv))
            {
                string fullName = topFn.ValueKind == JsonValueKind.String ? topFn.GetString()! : "未知房间";
                string verify = topRv.ValueKind == JsonValueKind.String ? topRv.GetString()! : "";

                if (!string.IsNullOrEmpty(verify))
                {
                    double oddVal = 0.0;
                    double useVal = 0.0;
                    int statusVal = 0;

                    if (innerRoot.TryGetProperty("detaillist", out var detailList) && detailList.ValueKind == JsonValueKind.Array && detailList.GetArrayLength() > 0)
                    {
                        var detail = detailList[0];

                        if (detail.TryGetProperty("odd", out var oEl))
                        {
                            oddVal = oEl.ValueKind == JsonValueKind.String
                                ? (double.TryParse(oEl.GetString(), out var d) ? d : 0)
                                : (oEl.ValueKind == JsonValueKind.Number ? oEl.GetDouble() : 0);
                        }

                        if (detail.TryGetProperty("use", out var uEl))
                        {
                            useVal = uEl.ValueKind == JsonValueKind.String
                                ? (double.TryParse(uEl.GetString(), out var d) ? d : 0)
                                : (uEl.ValueKind == JsonValueKind.Number ? uEl.GetDouble() : 0);
                        }

                        if (detail.TryGetProperty("status", out var sEl))
                        {
                            if (sEl.ValueKind == JsonValueKind.String)
                            {
                                string sStr = sEl.GetString()!;
                                int.TryParse(sStr.Trim(), out statusVal);
                            }
                            else if (sEl.ValueKind == JsonValueKind.Number)
                            {
                                statusVal = sEl.GetInt32();
                            }
                        }
                    }
                    else
                    {
                        if (innerRoot.TryGetProperty("odd", out var oEl))
                        {
                            oddVal = oEl.ValueKind == JsonValueKind.String
                                ? (double.TryParse(oEl.GetString(), out var d) ? d : 0)
                                : (oEl.ValueKind == JsonValueKind.Number ? oEl.GetDouble() : 0);
                        }

                        if (innerRoot.TryGetProperty("use", out var uEl))
                        {
                            useVal = uEl.ValueKind == JsonValueKind.String
                                ? (double.TryParse(uEl.GetString(), out var d) ? d : 0)
                                : (uEl.ValueKind == JsonValueKind.Number ? uEl.GetDouble() : 0);
                        }

                        if (innerRoot.TryGetProperty("status", out var sEl))
                        {
                            if (sEl.ValueKind == JsonValueKind.String)
                            {
                                string sStr = sEl.GetString()!;
                                int.TryParse(sStr.Trim(), out statusVal);
                            }
                            else if (sEl.ValueKind == JsonValueKind.Number)
                            {
                                statusVal = sEl.GetInt32();
                            }
                        }
                    }

                    resultList.Add(new RoomInfo
                    {
                        RoomName = fullName,
                        RoomVerify = verify,
                        Odd = oddVal,
                        Use = useVal,
                        Status = statusVal
                    });

                    return resultList;
                }
            }

            return null;
        }
        catch (JsonException je)
        {
            Console.WriteLine($"❌ JSON 解析错误: {je.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 解析过程异常: {ex.Message}");
            return null;
        }
    }
}
