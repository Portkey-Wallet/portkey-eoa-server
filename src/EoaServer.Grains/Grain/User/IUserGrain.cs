namespace EoaServer.Grain.User;


public interface IUserGrain: IGrainWithGuidKey
{
    Task<GrainResultDto<UserGrainDto>> Create(UserGrainDto userGrainDto);
    Task<GrainResultDto<UserGrainDto>> UpdateWalletName(string walletName);
}