using Aiursoft.Translate.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Aiursoft.Translate.Services;

/// <summary>
/// Tracks per-user and per-IP translation counts within a rolling 1-hour window.
/// Limits are read from GlobalSettings so they can be configured at runtime.
/// </summary>
public class GuestTranslateRateLimiter(IMemoryCache cache, GlobalSettingsService settings)
{
    private sealed class RateEntry
    {
        public int Count;
    }

    private bool TryConsume(string cacheKey, int maxPerHour)
    {
        var entry = cache.GetOrCreate(cacheKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return new RateEntry { Count = 0 };
        })!;

        if (entry.Count >= maxPerHour)
            return false;

        entry.Count++;
        return true;
    }

    /// <summary>
    /// Attempts to consume one quota unit for an anonymous IP.
    /// </summary>
    public async Task<bool> TryConsumeAsGuestAsync(string ip)
    {
        var max = await settings.GetIntSettingAsync(SettingsMap.GuestTranslateMaxPerHour);
        return TryConsume($"guest_translate:{ip}", max);
    }

    /// <summary>
    /// Attempts to consume one quota unit for an authenticated user.
    /// </summary>
    public async Task<bool> TryConsumeAsUserAsync(string userId)
    {
        var max = await settings.GetIntSettingAsync(SettingsMap.UserTranslateMaxPerHour);
        return TryConsume($"user_translate:{userId}", max);
    }
}
