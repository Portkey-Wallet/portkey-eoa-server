using System.ComponentModel.DataAnnotations;

namespace EoaServer.UserAssets;

public class SearchUserAssetsRequestDto : GetAssetsBase
{
    [MaxLength(100)] public string Keyword { get; set; }
    
    public int Width { get; set; }
    
    public int Height { get; set; }
}