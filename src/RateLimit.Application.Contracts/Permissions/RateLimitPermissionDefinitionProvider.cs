using RateLimit.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace RateLimit.Permissions;

public class RateLimitPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(RateLimitPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(RateLimitPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<RateLimitResource>(name);
    }
}
