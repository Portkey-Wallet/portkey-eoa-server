using System.ComponentModel.DataAnnotations;

namespace EoaServer.Transfer.Dtos;

public class GetDepositRequestDto
{
    public string ChainId { get; set; }
    public string Network { get; set; }
    [Required] public string Symbol { get; set; }
    [Required] public string? ToSymbol { get; set; }
}