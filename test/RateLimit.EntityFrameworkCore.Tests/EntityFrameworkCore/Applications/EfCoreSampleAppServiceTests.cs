using RateLimit.Samples;
using Xunit;

namespace RateLimit.EntityFrameworkCore.Applications;

[Collection(RateLimitTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<RateLimitEntityFrameworkCoreTestModule>
{

}
