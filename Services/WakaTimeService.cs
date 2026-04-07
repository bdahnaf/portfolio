using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Portfolio.Models;

namespace Portfolio.Services;

public class WakaTimeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public WakaTimeService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["WakaTime:ApiKey"] 
                  ?? throw new InvalidOperationException("WakaTime API Key is missing from configuration.");
    }

    public async Task<List<WakaLanguage>> GetWeeklyStatsAsync()
    {
        // WakaTime requires the API key to be Base64 encoded for Basic Authentication
        var base64Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(_apiKey));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Key);

        var response = await _httpClient.GetAsync("https://wakatime.com/api/v1/users/current/stats/last_7_days");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WakaTimeResponse>(content);
            
            // Return top 5 languages, ignoring "Other" or generic text
            return result?.Data?.Languages
                .Where(l => l.Name != "Other")
                .Take(5)
                .ToList() ?? new List<WakaLanguage>();
        }

        return new List<WakaLanguage>();
    }
}