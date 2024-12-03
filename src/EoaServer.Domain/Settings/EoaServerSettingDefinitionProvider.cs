using Volo.Abp.Settings;

namespace EoaServer.Settings;

public class EoaServerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(EoaServerSettings.MySetting1));
    }
}
