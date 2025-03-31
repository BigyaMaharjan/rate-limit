using RateLimit.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace RateLimit.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class RateLimitController : AbpControllerBase
{
    protected RateLimitController()
    {
        LocalizationResource = typeof(RateLimitResource);
    }
}
