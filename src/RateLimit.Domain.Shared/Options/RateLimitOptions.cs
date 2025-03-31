using System.Collections.Generic;

namespace RateLimit.Options;
public class RateLimitOptions
{
    public int RequestsPerMinute { get; set; }
    public List<string> WhitelistedIps { get; set; } = new List<string>();
    public int CacheExpiryTimeMinutes { get; set; }
}