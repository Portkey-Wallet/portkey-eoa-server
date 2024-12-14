namespace EoaServer.Grain.UserToken;

public interface IUserTokenGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UserTokenGrainDto>> GetAsync();
    Task<GrainResultDto<UserTokenGrainDto>> AddAsync(Guid userId, UserTokenGrainDto tokenItem);
    Task<GrainResultDto<UserTokenGrainDto>> ChangeDisplayAsync(Guid userId, bool isDisplay, bool isDelete = false);
}