using Microsoft.Extensions.Localization;
using RateLimit.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace RateLimit.Web;

[Dependency(ReplaceServices = true)]
public class RateLimitBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<RateLimitResource> _localizer;

    public RateLimitBrandingProvider(IStringLocalizer<RateLimitResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
