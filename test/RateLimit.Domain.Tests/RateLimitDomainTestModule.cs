using Volo.Abp.Modularity;

namespace RateLimit;

[DependsOn(
    typeof(RateLimitDomainModule),
    typeof(RateLimitTestBaseModule)
)]
public class RateLimitDomainTestModule : AbpModule
{

}
