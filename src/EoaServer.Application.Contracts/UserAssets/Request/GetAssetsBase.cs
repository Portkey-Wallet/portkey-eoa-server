using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Volo.Abp.Application.Dtos;

namespace EoaServer.UserAssets;

public class GetAssetsBase : PagedResultRequestDto
{
    public List<AddressInfo> AddressInfos { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (AddressInfos.IsNullOrEmpty() ||
            AddressInfos.Any(info => info.Address.IsNullOrEmpty() || info.ChainId.IsNullOrEmpty()))
        {
            yield return new ValidationResult("Invalid Addresses or AddressInfos input.");
        }
    }
}

public class AddressInfo
{
    public string Address { get; set; }
    public string ChainId { get; set; }
}