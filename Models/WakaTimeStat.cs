using System.Text.Json.Serialization;

namespace Portfolio.Models;

public class WakaTimeResponse
{
    [JsonPropertyName("data")]
    public WakaData Data { get; set; } = new();
}

public class WakaData
{
    [JsonPropertyName("languages")]
    public List<WakaLanguage> Languages { get; set; } = new();
}

public class WakaLanguage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string TimeSpent { get; set; } = string.Empty; // e.g., "14 hrs 30 mins"

    [JsonPropertyName("percent")]
    public double Percent { get; set; }
}