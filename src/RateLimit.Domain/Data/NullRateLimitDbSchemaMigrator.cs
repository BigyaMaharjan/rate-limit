using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace RateLimit.Data;

/* This is used if database provider does't define
 * IRateLimitDbSchemaMigrator implementation.
 */
public class NullRateLimitDbSchemaMigrator : IRateLimitDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
