using Volo.Abp.Modularity;

namespace RateLimit;

[DependsOn(
    typeof(RateLimitApplicationModule),
    typeof(RateLimitDomainTestModule)
)]
public class RateLimitApplicationTestModule : AbpModule
{

}
