using System.Text.Json.Nodes;

namespace Portfolio.Services;

public class SteamService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public SteamService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<object?> GetTopGamesAsync()
    {
        try
        {
            var apiKey = _config["Steam:ApiKey"];
            var steamId = _config["Steam:SteamId"];

            // Steam API: Get all owned games, including free ones, with game names/icons
            var url = $"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={apiKey}&steamid={steamId}&include_appinfo=1&include_played_free_games=1";

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Steam API Failed: {response.StatusCode} - {error}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(content);
            
            var gamesArray = json?["response"]?["games"]?.AsArray();

            if (gamesArray == null || gamesArray.Count == 0)
            {
                return new { success = false, errorMessage = "No games found. Make sure 'Game details' is Public in Steam privacy settings." };
            }

            // Sort by playtime (minutes), take the top 2, and format the data
            var topGames = gamesArray
                .Select(g => new
                {
                    AppId = g["appid"]?.ToString(),
                    Name = g["name"]?.ToString(),
                    PlaytimeMinutes = g["playtime_forever"]?.GetValue<int>() ?? 0
                })
                .OrderByDescending(g => g.PlaytimeMinutes)
                .Take(2)
                .Select(g => new
                {
                    title = g.Name,
                    hoursPlayed = Math.Round(g.PlaytimeMinutes / 60.0, 1).ToString("N1"), // E.g., "1,200.5"
                    bannerUrl = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{g.AppId}/header.jpg",
                    storeUrl = $"https://store.steampowered.com/app/{g.AppId}/"
                })
                .ToList();

            return new
            {
                success = true,
                profileUrl = $"https://steamcommunity.com/profiles/{steamId}/",
                // This special steam:// protocol forces the native Steam app to open the "Add Friend" dialogue!
                addFriendUrl = $"steam://friends/add/{steamId}", 
                games = topGames
            };
        }
        catch (Exception ex)
        {
            return new { success = false, hasError = true, errorMessage = ex.Message };
        }
    }
}