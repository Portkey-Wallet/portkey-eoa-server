using System.ComponentModel.DataAnnotations;

namespace EoaServer.UserAssets;

public class GetNftItemRequestDto : GetAssetsBase
{
    [Required][MinLength(1)] public string Symbol { get; set; }
    
    public int Width { get; set; }
    
    public int Height { get; set; }
}