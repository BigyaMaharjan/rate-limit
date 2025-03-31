using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using RateLimit.CacheKeys;
using RateLimit.Options;
using RateLimit.RequestCount;
using System;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Timing;

namespace RateLimit;

public class RateLimitMiddleware : IMiddleware, ITransientDependency
{
    private readonly IDistributedCache<RateLimitRequestCount> _cache;
    private readonly RateLimitOptions _options;
    private readonly IClock _clock;


    public RateLimitMiddleware(IDistributedCache<RateLimitRequestCount> cache, IOptions<RateLimitOptions> options, IClock clock)
    {
        _cache = cache;
        _options = options.Value;
        _clock = clock;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Only apply rate limiting to API endpoints
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(clientIp) || _options.WhitelistedIps.Contains(clientIp))
        {
            await next(context);
            return;
        }

        var cacheKey = $"{clientIp}:{RateLimitCacheKey.RATE_LIMIT_CACHE_KEY}";
        var requestCount = await _cache.GetOrAddAsync(
                cacheKey,
                () => Task.FromResult(new RateLimitRequestCount
                {
                    Count = 0,
                    WindowEnd = _clock.Now.AddMinutes(_options.CacheExpiryTimeMinutes)
                }),
                () => new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_options.CacheExpiryTimeMinutes)
                });

        // Calculate retryAfter in minutes
        var retryAfterMinutes = (requestCount.WindowEnd - _clock.Now).TotalMinutes;
        if (_clock.Now > requestCount.WindowEnd || retryAfterMinutes <= 0)
        {
            requestCount.Count = 0;
            requestCount.WindowEnd = _clock.Now.AddMinutes(_options.CacheExpiryTimeMinutes);
            retryAfterMinutes = _options.CacheExpiryTimeMinutes; 
        }

        if (requestCount.Count >= _options.RequestsPerMinute)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = $"Limited to {_options.RequestsPerMinute} requests per minute",
                retryAfter = (int)(requestCount.WindowEnd - _clock.Now).TotalSeconds
            });
            return;
        }

        requestCount.Count++;

        await _cache.SetAsync(cacheKey, requestCount, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(1)
        });

        await next(context);
    }
}
