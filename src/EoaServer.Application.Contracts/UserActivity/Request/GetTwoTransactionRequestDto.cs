using System.Collections.Generic;
using EoaServer.UserActivity.Dtos;
using EoaServer.UserAssets;

namespace EoaServer.UserActivity.Dto;

public class GetTwoTransactionRequestDto : GetActivitiesRequestDto
{
    public List<AddressInfo> TargetAddressInfos { get; set; }
    
}