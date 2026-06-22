using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WaterElectricityAutoClient;

public static class SettingsService
{
    public static string GetConfigPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }

    public static JsonDocument LoadDocument()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
            throw new FileNotFoundException($"配置文件未找到: {path}");

        var json = File.ReadAllText(path);
        return JsonDocument.Parse(json);
    }

    public static EmailSettings LoadEmailSettings()
    {
        using var doc = LoadDocument();
        var root = doc.RootElement;
        if (root.TryGetProperty("EmailSettings", out var el))
            return JsonSerializer.Deserialize<EmailSettings>(el.GetRawText()) ?? new EmailSettings();
        return new EmailSettings();
    }

    public static QuerySettings LoadQuerySettings()
    {
        using var doc = LoadDocument();
        var root = doc.RootElement;
        if (root.TryGetProperty("QuerySettings", out var el))
            return JsonSerializer.Deserialize<QuerySettings>(el.GetRawText()) ?? new QuerySettings();
        return new QuerySettings();
    }

    public static DengDengSettings LoadDengDengSettings()
    {
        using var doc = LoadDocument();
        var root = doc.RootElement;
        if (root.TryGetProperty("DengDengSettings", out var el))
            return JsonSerializer.Deserialize<DengDengSettings>(el.GetRawText()) ?? new DengDengSettings();
        return new DengDengSettings();
    }

    public static MqttSettings LoadMqttSettings()
    {
        using var doc = LoadDocument();
        var root = doc.RootElement;
        if (root.TryGetProperty("MqttSettings", out var el))
            return JsonSerializer.Deserialize<MqttSettings>(el.GetRawText()) ?? new MqttSettings();
        return new MqttSettings();
    }

    public static void SaveAllSettings(EmailSettings email, QuerySettings query, DengDengSettings dengDeng, MqttSettings mqtt)
    {
        var path = GetConfigPath();
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        // Track which known sections were found in the file
        var written = new HashSet<string>();

        foreach (var prop in root.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "EmailSettings":
                    WriteSettingsObject(writer, prop.Name, SerializeSection(email));
                    written.Add(prop.Name);
                    break;
                case "QuerySettings":
                    WriteSettingsObject(writer, prop.Name, SerializeSection(query));
                    written.Add(prop.Name);
                    break;
                case "DengDengSettings":
                    WriteSettingsObject(writer, prop.Name, SerializeSection(dengDeng));
                    written.Add(prop.Name);
                    break;
                case "MqttSettings":
                    WriteSettingsObject(writer, prop.Name, SerializeSection(mqtt));
                    written.Add(prop.Name);
                    break;
                default:
                    prop.WriteTo(writer);
                    break;
            }
        }

        // Ensure all known sections exist even if not in the file yet
        WriteIfMissing(writer, "EmailSettings", email, written);
        WriteIfMissing(writer, "QuerySettings", query, written);
        WriteIfMissing(writer, "DengDengSettings", dengDeng, written);
        WriteIfMissing(writer, "MqttSettings", mqtt, written);

        writer.WriteEndObject();
        writer.Flush();

        File.WriteAllText(path, JsonSerializer.Serialize(JsonDocument.Parse(stream.ToArray()).RootElement, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static JsonElement SerializeSection<T>(T section)
    {
        return JsonSerializer.SerializeToElement(section, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static void WriteIfMissing<T>(Utf8JsonWriter writer, string name, T section, HashSet<string> written)
    {
        if (!written.Contains(name))
        {
            WriteSettingsObject(writer, name, SerializeSection(section));
        }
    }

    public static void SaveQuerySettings(QuerySettings settings)
    {
        var email = LoadEmailSettings();
        var dengDeng = LoadDengDengSettings();
        var mqtt = LoadMqttSettings();
        SaveAllSettings(email, settings, dengDeng, mqtt);
    }

    public static void SaveEmailSettings(EmailSettings settings)
    {
        var query = LoadQuerySettings();
        var dengDeng = LoadDengDengSettings();
        var mqtt = LoadMqttSettings();
        SaveAllSettings(settings, query, dengDeng, mqtt);
    }

    public static void SaveDengDengSettings(DengDengSettings settings)
    {
        var email = LoadEmailSettings();
        var query = LoadQuerySettings();
        var mqtt = LoadMqttSettings();
        SaveAllSettings(email, query, settings, mqtt);
    }

    public static void SaveMqttSettings(MqttSettings settings)
    {
        var email = LoadEmailSettings();
        var query = LoadQuerySettings();
        var dengDeng = LoadDengDengSettings();
        SaveAllSettings(email, query, dengDeng, settings);
    }

    public static string LoadTheme()
    {
        try
        {
            using var doc = LoadDocument();
            if (doc.RootElement.TryGetProperty("Theme", out var el))
            {
                var theme = el.GetString();
                if (theme == "Dark" || theme == "Light")
                    return theme;
            }
        }
        catch { }
        return "Light";
    }

    public static void SaveTheme(string theme)
    {
        var path = GetConfigPath();
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("Theme", theme);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name != "Theme")
                prop.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        File.WriteAllText(path, JsonSerializer.Serialize(
            JsonDocument.Parse(stream.ToArray()).RootElement,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void WriteSettingsObject(Utf8JsonWriter writer, string propertyName, JsonElement value)
    {
        writer.WritePropertyName(propertyName);
        value.WriteTo(writer);
    }
}
