using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RateLimit.CacheKeys;
using RateLimit.Options;
using RateLimit.RequestCount;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Timing;

namespace RateLimit.Middlewares;

public class RateLimitMiddleware : IMiddleware, ITransientDependency
{
    private readonly IDistributedCache<RateLimitRequestCount> _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimitOptions _options;
    private readonly IClock _clock;
    private readonly IWebHostEnvironment _env;

    public RateLimitMiddleware(
        IDistributedCache<RateLimitRequestCount> cache,
        IConnectionMultiplexer redis,
        IOptions<RateLimitOptions> options,
        IClock clock,
        IWebHostEnvironment env)
    {
        _cache = cache;
        _redis = redis;
        _options = options.Value;
        _clock = clock;
        _env = env;
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

        var endpoint = context.Request.Path.ToString().ToLower();
        var limit = _options.EndpointLimits.TryGetValue(endpoint, out int configLimit)
            ? configLimit
            : _options.RequestsPerMinute;

        var cacheKey = $"{clientIp}:{RateLimitCacheKey.RATE_LIMIT_CACHE_KEY}";
        var backoffKey = $"{clientIp}:{RateLimitCacheKey.RATE_LIMIT_BACKOFF_KEY}";

        var db = _redis.GetDatabase();

        var backoffAttempts = await db.StringGetAsync(backoffKey);
        int backoffMinutes = backoffAttempts.HasValue ? (int)Math.Pow(2, (int)backoffAttempts) : 0;
        if (backoffMinutes > 0)
        {
            var backoffExpiry = await db.KeyTimeToLiveAsync(backoffKey);
            if (backoffExpiry.HasValue && backoffExpiry.Value.TotalMinutes > 0)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ((int)(backoffExpiry.Value.TotalMinutes * 60)).ToString(); // Set header first
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded with backoff",
                    message = $"Blocked due to repeated violations. Wait {backoffMinutes} minute(s).",
                    retryAfterMinutes = Math.Round(backoffExpiry.Value.TotalMinutes, 2)
                });
                return;
            }
        }

        // Atomic increment for rate limiting
        var count = await db.StringIncrementAsync(cacheKey);
        if (count == 1)
        {
            await db.KeyExpireAsync(cacheKey, TimeSpan.FromMinutes(_options.CacheExpiryTimeMinutes));
        }

        if (count > limit)
        {
            // Increment backoff attempts and set expiry
            var attempts = await db.StringIncrementAsync(backoffKey);
            var newBackoffMinutes = Math.Pow(2, attempts);
            await db.KeyExpireAsync(backoffKey, TimeSpan.FromMinutes(newBackoffMinutes));

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = ((int)(newBackoffMinutes * 60)).ToString(); // Set header first
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = $"Limited to {limit} requests per {_options.CacheExpiryTimeMinutes} minute(s) for {endpoint}",
                retryAfterMinutes = Math.Round(newBackoffMinutes, 2)
            });
            return;
        }

        await next(context);
    }
}
