using System;
using System.Collections.Generic;
using System.Text;
using RateLimit.Localization;
using Volo.Abp.Application.Services;

namespace RateLimit;

/* Inherit your application services from this class.
 */
public abstract class RateLimitAppService : ApplicationService
{
    protected RateLimitAppService()
    {
        LocalizationResource = typeof(RateLimitResource);
    }
}
