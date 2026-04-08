using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Portfolio.Services;

public class SpotifyService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public SpotifyService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var clientId = _config["Spotify:ClientId"];
        var clientSecret = _config["Spotify:ClientSecret"];
        var refreshToken = _config["Spotify:RefreshToken"];

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        
        var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken! }
        });

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Spotify Auth Failed [{response.StatusCode}]: {errorBody}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        return json?["access_token"]?.ToString() ?? string.Empty;
    }

    public async Task<object?> GetCurrentlyPlayingAsync()
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/currently-playing");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return new { isPlaying = false };
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Spotify Data Failed [{response.StatusCode}]: {errorBody}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(content);

            if (json?["is_playing"]?.GetValue<bool>() != true || json["item"]?["type"]?.ToString() != "track")
            {
                return new { isPlaying = false };
            }

            return new
            {
                isPlaying = true,
                title = json["item"]?["name"]?.ToString(),
                artist = json["item"]?["artists"]?[0]?["name"]?.ToString(),
                albumArt = json["item"]?["album"]?["images"]?[0]?["url"]?.ToString(),
                spotifyUrl = json["item"]?["external_urls"]?["spotify"]?.ToString()
            };
        }
        catch (Exception ex)
        {
            return new { 
                isPlaying = false, 
                hasError = true, 
                errorMessage = ex.Message 
            };
        }
    }
}