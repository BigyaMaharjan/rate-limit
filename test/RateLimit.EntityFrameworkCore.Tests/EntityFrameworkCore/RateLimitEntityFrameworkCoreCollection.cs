using Xunit;

namespace RateLimit.EntityFrameworkCore;

[CollectionDefinition(RateLimitTestConsts.CollectionDefinitionName)]
public class RateLimitEntityFrameworkCoreCollection : ICollectionFixture<RateLimitEntityFrameworkCoreFixture>
{

}
