using System.Threading.Tasks;
using Asp.Versioning;
using EoaServer.Transfer;
using EoaServer.Transfer.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace EoaServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Transfer")]
[Route("api/app/transfer")]
// [Authorize]
public class TransferController : EoaServerBaseController
{
    private readonly IShiftChainService _shiftChainService;
    private readonly ITransferAppService _transferAppService;

    public TransferController(IShiftChainService shiftChainService,
        ITransferAppService transferAppService)
    {
        _shiftChainService = shiftChainService;
        _transferAppService = transferAppService;
    }
    
    [HttpPost("connect/token")]
    public async Task<AuthTokenDto> GetConnectTokenAsync([FromForm] AuthTokenRequestDto request)
    {
        return await _transferAppService.GetConnectTokenAsync(request);
    }


    [HttpGet("token/list")]
    public async Task<WithdrawTokenListDto> GetTokenListAsync(WithdrawTokenListRequestDto request)
    {
        return (await _transferAppService.GetTokenListAsync(request)).Data;
    }

    [HttpGet("token/option")]
    public async Task<GetTokenOptionListDto> GetTokenOptionListAsync(GetTokenOptionListRequestDto request)
    {
        return (await _transferAppService.GetTokenOptionListAsync(request)).Data;
    }

    [HttpGet("network/list")]
    public async Task<GetNetworkListDto> GetNetworkListAsync(GetNetworkListRequestDto request)
    {
        return (await _transferAppService.GetNetworkListAsync(request)).Data;
    }

    [HttpGet("getReceiveNetworkList")]
    public async Task<ReceiveNetworkDto> GetNetworkListBySymbolAsync(
        GetReceiveNetworkListRequestDto request)
    {
        return (await _shiftChainService.GetReceiveNetworkList(request)).Data;
    }

    [HttpGet("getSendNetworkList")]
    public async Task<SendNetworkDto> GetDestinationList(GetSendNetworkListRequestDto request)
    {
        return (await _shiftChainService.GetSendNetworkList(request)).Data;
    }
    
    [HttpGet("deposit/calculator")]
    public async Task<CalculateDepositRateDto> CalculateDepositRateAsync(GetCalculateDepositRateRequestDto request)
    {
        return (await _transferAppService.CalculateDepositRateAsync(request)).Data;
    }

    [HttpGet("deposit/info")]
    public async Task<GetDepositInfoDto> GetDepositInfoAsync(GetDepositRequestDto request)
    {
        return (await _transferAppService.GetDepositInfoAsync(request)).Data;
    }
    
    [HttpGet("network/tokens")]
    public async Task<GetNetworkTokensDto> GetNetworkTokensAsync(GetNetworkTokensRequestDto request)
    {
        return (await _transferAppService.GetNetworkTokensAsync(request)).Data;
    }
    
    [HttpGet("record/list")]
    public async Task<PagedResultDto<OrderIndexDto>> GetRecordListAsync(GetOrderRecordRequestDto request)
    {
        return (await _transferAppService.GetRecordListAsync(request)).Data;
    }
}