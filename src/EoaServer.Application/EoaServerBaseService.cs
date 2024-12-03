using EoaServer.Localization;
using Volo.Abp.Application.Services;

namespace EoaServer;

/* Inherit your application services from this class.
 */
public abstract class EoaServerBaseService : ApplicationService
{
    protected EoaServerBaseService()
    {
        LocalizationResource = typeof(EoaServerResource);
    }
}
