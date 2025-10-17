using System.Collections.Generic;

namespace EoaServer.Awaken;

public class AwakenSupportedTokenResponse
{    
    public long Total { get; set; }
    public List<UserAssets.Dtos.Token> Data { get; set; }
}