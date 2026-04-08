using Microsoft.Extensions.Caching.Memory;

namespace Aiursoft.Translate.Services;

/// <summary>
/// Tracks how many times an anonymous IP has triggered a translation within a rolling 1-hour window.
/// Allows up to <see cref="MaxPerHour"/> translations before blocking.
/// </summary>
public class GuestTranslateRateLimiter(IMemoryCache cache)
{
    public const int MaxPerHour = 3;

    private sealed class RateEntry
    {
        public int Count;
    }

    /// <summary>
    /// Attempts to consume one quota unit for the given IP.
    /// Returns <c>true</c> when the request is within the allowed limit, <c>false</c> when the limit is exceeded.
    /// </summary>
    public bool TryConsume(string ip)
    {
        var key = $"guest_translate:{ip}";

        var entry = cache.GetOrCreate(key, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return new RateEntry { Count = 0 };
        })!;

        if (entry.Count >= MaxPerHour)
            return false;

        entry.Count++;
        return true;
    }
}
