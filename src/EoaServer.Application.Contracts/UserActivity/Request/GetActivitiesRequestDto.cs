using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EoaServer.UserAssets;
using Volo.Abp.Application.Dtos;

namespace EoaServer.UserActivity.Dtos;

public class GetActivitiesRequestDto : PagedResultRequestDto
{
    public string Address { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Address.IsNullOrEmpty())
        {
            yield return new ValidationResult("Invalid Address input.");
        }
    }
}