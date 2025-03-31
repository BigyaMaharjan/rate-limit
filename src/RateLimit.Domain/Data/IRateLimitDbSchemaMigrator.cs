using System.Threading.Tasks;

namespace RateLimit.Data;

public interface IRateLimitDbSchemaMigrator
{
    Task MigrateAsync();
}
