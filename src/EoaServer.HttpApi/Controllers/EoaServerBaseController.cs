using EoaServer.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace EoaServer.Controllers;

public abstract class EoaServerBaseController : AbpControllerBase
{
    protected EoaServerBaseController()
    {
        LocalizationResource = typeof(EoaServerResource);
    }
}