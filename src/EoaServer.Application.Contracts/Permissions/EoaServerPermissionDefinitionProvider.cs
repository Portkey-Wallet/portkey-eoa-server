using EoaServer.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace EoaServer.Permissions;

public class EoaServerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        context.AddGroup(EoaServerPermissions.GroupName);
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<EoaServerResource>(name);
    }
}