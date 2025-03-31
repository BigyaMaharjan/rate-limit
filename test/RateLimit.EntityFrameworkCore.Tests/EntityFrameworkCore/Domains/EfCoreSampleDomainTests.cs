using RateLimit.Samples;
using Xunit;

namespace RateLimit.EntityFrameworkCore.Domains;

[Collection(RateLimitTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<RateLimitEntityFrameworkCoreTestModule>
{

}
