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
    public string ActivityType { get; set; }

    public List<string> Addresses { get; set; }

    public List<AddressInfo> AddressInfos { get; set; }

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

        if ((AddressInfos.IsNullOrEmpty() ||
             AddressInfos.Any(info => info.Address.IsNullOrEmpty() || info.ChainId.IsNullOrEmpty())) &&
            (Addresses == null || Addresses.Count == 0 || Addresses.Any(string.IsNullOrEmpty)))
        {
            yield return new ValidationResult("Invalid Addresses or AddressInfos input.");
        }
    }
}