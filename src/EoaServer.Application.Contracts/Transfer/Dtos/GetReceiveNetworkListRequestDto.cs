using System.ComponentModel.DataAnnotations;

namespace EoaServer.Transfer.Dtos;

public class GetReceiveNetworkListRequestDto
{
    [Required] public string Symbol { get; set; }
}