using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EoaServer.UserAssets;

namespace EoaServer.UserActivity.Dto;

public class GetActivityRequestDto : IValidatableObject
{
    [Required] public string TransactionId { get; set; }
    [Required] public string ChainId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(TransactionId))
        {
            yield return new ValidationResult("Invalid TransactionId input.");
        }

        if (string.IsNullOrEmpty(ChainId))
        {
            yield return new ValidationResult("Invalid ChainId input.");
        }
    }
}