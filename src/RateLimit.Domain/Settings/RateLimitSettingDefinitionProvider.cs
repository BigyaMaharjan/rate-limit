using Volo.Abp.Settings;

namespace RateLimit.Settings;

public class RateLimitSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(RateLimitSettings.MySetting1));
    }
}
