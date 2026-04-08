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
    public async Task<object?> GetSpotifyDashboardAsync()
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            // 1. Prepare both requests
            var nowPlayingReq = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/currently-playing");
            nowPlayingReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // time_range=short_term gives approximately the last 4 weeks (1 month)
            var topTracksReq = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/top/tracks?time_range=short_term&limit=3");
            topTracksReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // 2. Fire both requests simultaneously
            var nowPlayingTask = _httpClient.SendAsync(nowPlayingReq);
            var topTracksTask = _httpClient.SendAsync(topTracksReq);

            await Task.WhenAll(nowPlayingTask, topTracksTask);

            var npResponse = await nowPlayingTask;
            var ttResponse = await topTracksTask;

            // 3. Parse Now Playing
            object nowPlayingObj = new { isPlaying = false };
            if (npResponse.IsSuccessStatusCode && npResponse.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                var npContent = await npResponse.Content.ReadAsStringAsync();
                var npJson = JsonNode.Parse(npContent);
                if (npJson?["is_playing"]?.GetValue<bool>() == true && npJson["item"]?["type"]?.ToString() == "track")
                {
                    nowPlayingObj = new
                    {
                        isPlaying = true,
                        title = npJson["item"]?["name"]?.ToString(),
                        artist = npJson["item"]?["artists"]?[0]?["name"]?.ToString(),
                        albumArt = npJson["item"]?["album"]?["images"]?[0]?["url"]?.ToString(),
                        spotifyUrl = npJson["item"]?["external_urls"]?["spotify"]?.ToString()
                    };
                }
            }

            // 4. Parse Top Tracks
            var topTracksList = new List<object>();
            if (ttResponse.IsSuccessStatusCode)
            {
                var ttContent = await ttResponse.Content.ReadAsStringAsync();
                var ttJson = JsonNode.Parse(ttContent);
                var items = ttJson?["items"]?.AsArray();

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        topTracksList.Add(new
                        {
                            title = item["name"]?.ToString(),
                            artist = item["artists"]?[0]?["name"]?.ToString(),
                            albumArt = item["album"]?["images"]?[0]?["url"]?.ToString(),
                            spotifyUrl = item["external_urls"]?["spotify"]?.ToString()
                        });
                    }
                }
            }
            else
            {
                var error = await ttResponse.Content.ReadAsStringAsync();
                throw new Exception($"Top Tracks Failed: {ttResponse.StatusCode} - {error}");
            }

            return new
            {
                success = true,
                nowPlaying = nowPlayingObj,
                topTracks = topTracksList
            };
        }
        catch (Exception ex)
        {
            return new { success = false, hasError = true, errorMessage = ex.Message };
        }
    }
}