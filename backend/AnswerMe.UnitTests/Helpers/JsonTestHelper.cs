using System.Text.Json;
using FluentAssertions;

namespace AnswerMe.UnitTests.Helpers;

/// <summary>
/// JSON 序列化测试辅助类
/// </summary>
public static class JsonTestHelper
{

    /// <summary>
    /// 序列化对象为 JSON (使用 camelCase)
    /// </summary>
    public static string Serialize<T>(T obj, bool writeIndented = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = writeIndented
        };

        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// 从 JSON 反序列化对象
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };

        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// 验证 JSON 字符串包含指定的路径和值
    /// </summary>
    public static void ShouldContainJsonPath(string json, string path, string expectedValue)
    {
        var doc = JsonDocument.Parse(json);
        var pathParts = path.Split('.');

        JsonElement current = doc.RootElement;

        foreach (var part in pathParts)
        {
            if (int.TryParse(part, out int index))
            {
                current = current.EnumerateArray().ElementAt(index);
            }
            else
            {
                current = current.GetProperty(part);
            }
        }

        current.GetString().Should().Be(expectedValue);
    }

    /// <summary>
    /// 验证 JSON 字符串是否有效
    /// </summary>
    public static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取 JSON 的格式化版本 (用于调试)
    /// </summary>
    public static string FormatJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// 比较两个 JSON 字符串是否等价 (忽略顺序和空格)
    /// </summary>
    public static bool JsonEquals(string json1, string json2)
    {
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);

        return JsonElementEquals(doc1.RootElement, doc2.RootElement);
    }

    private static bool JsonElementEquals(JsonElement element1, JsonElement element2)
    {
        if (element1.ValueKind != element2.ValueKind)
            return false;

        return element1.ValueKind switch
        {
            JsonValueKind.Object => JsonObjectEquals(element1, element2),
            JsonValueKind.Array => JsonArrayEquals(element1, element2),
            JsonValueKind.String => element1.GetString() == element2.GetString(),
            JsonValueKind.Number => element1.GetDecimal() == element2.GetDecimal(),
            JsonValueKind.True or JsonValueKind.False => element1.GetBoolean() == element2.GetBoolean(),
            JsonValueKind.Null => true,
            _ => true
        };
    }

    private static bool JsonObjectEquals(JsonElement obj1, JsonElement obj2)
    {
        var props1 = obj1.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var props2 = obj2.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

        if (props1.Count != props2.Count)
            return false;

        foreach (var prop in props1)
        {
            if (!props2.TryGetValue(prop.Key, out var value2))
                return false;

            if (!JsonElementEquals(prop.Value, value2))
                return false;
        }

        return true;
    }

    private static bool JsonArrayEquals(JsonElement arr1, JsonElement arr2)
    {
        var items1 = arr1.EnumerateArray().ToList();
        var items2 = arr2.EnumerateArray().ToList();

        if (items1.Count != items2.Count)
            return false;

        for (int i = 0; i < items1.Count; i++)
        {
            if (!JsonElementEquals(items1[i], items2[i]))
                return false;
        }

        return true;
    }
}
