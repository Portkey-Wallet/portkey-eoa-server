using EoaServer.UserAssets;

namespace EoaServer.UserAssets;

public class GetTokenRequestDto : GetAssetsBase
{
    public string Version { get; set; }
}