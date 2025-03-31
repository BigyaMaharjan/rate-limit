using Volo.Abp.Modularity;

namespace RateLimit;

public abstract class RateLimitApplicationTestBase<TStartupModule> : RateLimitTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
