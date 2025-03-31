using System;

namespace RateLimit.RequestCount;
public class RateLimitRequestCount
{
    public int Count { get; set; }
    public DateTime WindowEnd { get; set; }
}