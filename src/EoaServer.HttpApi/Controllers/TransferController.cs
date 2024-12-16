using System.Threading.Tasks;
using Asp.Versioning;
using EoaServer.Transfer;
using EoaServer.Transfer.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace EoaServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Transfer")]
[Route("api/app/transfer")]
// [Authorize]
public class TransferController : EoaServerBaseController
{
    private readonly IShiftChainService _shiftChainService;

    public TransferController(IShiftChainService shiftChainService)
    {
        _shiftChainService = shiftChainService;
    }

    [HttpGet("getReceiveNetworkList")]
    public async Task<ResponseWrapDto<ReceiveNetworkDto>> GetNetworkListBySymbolAsync(
        GetReceiveNetworkListRequestDto request)
    {
        return await _shiftChainService.GetReceiveNetworkList(request);
    }

    [HttpGet("getSendNetworkList")]
    public async Task<ResponseWrapDto<SendNetworkDto>> GetDestinationList(GetSendNetworkListRequestDto request)
    {
        return await _shiftChainService.GetSendNetworkList(request);
    }
}