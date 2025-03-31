using Volo.Abp.Modularity;

namespace RateLimit;

/* Inherit from this class for your domain layer tests. */
public abstract class RateLimitDomainTestBase<TStartupModule> : RateLimitTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
