using RateLimit.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace RateLimit.Web.Pages;

public abstract class RateLimitPageModel : AbpPageModel
{
    protected RateLimitPageModel()
    {
        LocalizationResourceType = typeof(RateLimitResource);
    }
}
