using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace EoaServer;

[Dependency(ReplaceServices = true)]
public class EoaServerBrandingProvider: DefaultBrandingProvider
{
    public override string AppName => "EoaServer";
}
