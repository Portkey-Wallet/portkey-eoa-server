using System.Threading.Tasks;
using EoaServer.Transfer.Dtos;

namespace EoaServer.Transfer;

public interface IShiftChainService
{
    Task InitAsync();
    Task<ResponseWrapDto<ReceiveNetworkDto>> GetReceiveNetworkList(GetReceiveNetworkListRequestDto request);
    Task<ResponseWrapDto<SendNetworkDto>> GetSendNetworkList(GetSendNetworkListRequestDto request);
    //Task<GetSupportNetworkDto> GetSupportNetworkListAsync();
}