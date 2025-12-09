using System.Collections.Generic;

namespace EoaServer.UserAssets;

public class GetNftCollectionsRequestDto : GetAssetsBase
{
    public int Width { get; set; }
    
    public int Height { get; set; }
}