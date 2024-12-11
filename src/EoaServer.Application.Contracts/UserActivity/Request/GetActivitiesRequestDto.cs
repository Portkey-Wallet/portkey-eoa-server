using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EoaServer.UserAssets;
using Volo.Abp.Application.Dtos;

namespace EoaServer.UserActivity.Dtos;

public class GetActivitiesRequestDto : PagedResultRequestDto
{
    public List<AddressInfo> AddressInfos { get; set; }
    public List<string> TransactionTypes { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AddressInfos.IsNullOrEmpty() ||
            AddressInfos.Any(info => info.Address.IsNullOrEmpty() || info.ChainId.IsNullOrEmpty()))
        {
            yield return new ValidationResult("Invalid CaAddresses or CaAddressInfos input.");
        }
    }
}