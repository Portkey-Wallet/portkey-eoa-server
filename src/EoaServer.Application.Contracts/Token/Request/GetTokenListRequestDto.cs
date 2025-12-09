using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EoaServer.Token.Request;

public class GetTokenListRequestDto : IValidatableObject
{
    [Required] public string Symbol { get; set; }
    public List<string> ChainIds { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 200;
    public string Version { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainIds == null || ChainIds.Count == 0)
        {
            yield return new ValidationResult(
                "Invalid type input.",
                new[] { "ChainIds" }
            );
        }
    }
}