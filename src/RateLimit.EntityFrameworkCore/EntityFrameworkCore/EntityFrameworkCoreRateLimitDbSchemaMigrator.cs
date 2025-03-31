using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RateLimit.Data;
using Volo.Abp.DependencyInjection;

namespace RateLimit.EntityFrameworkCore;

public class EntityFrameworkCoreRateLimitDbSchemaMigrator
    : IRateLimitDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreRateLimitDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the RateLimitDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<RateLimitDbContext>()
            .Database
            .MigrateAsync();
    }
}
