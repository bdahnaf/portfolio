using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using portfolio.Models;
using Portfolio.Services;

namespace portfolio.Controllers;

public class HomeController : Controller
{
    private readonly WakaTimeService _wakaTimeService;
    private readonly IMemoryCache _cache;

    public HomeController(WakaTimeService wakaTimeService, IMemoryCache cache)
    {
        _wakaTimeService = wakaTimeService;
        _cache = cache;
    }
    
    public IActionResult Index()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet("api/coding-stats")]
    public async Task<IActionResult> GetCodingStats()
    {
        // Cache the result for 2 hours so you don't hit API limits
        if (!_cache.TryGetValue("WakaStats", out var stats))
        {
            stats = await _wakaTimeService.GetWeeklyStatsAsync();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(2));
                
            _cache.Set("WakaStats", stats, cacheOptions);
        }

        return Json(stats);
    }
}
