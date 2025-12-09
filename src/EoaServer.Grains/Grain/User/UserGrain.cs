using EoaServer.State.User;
using Volo.Abp.ObjectMapping;

namespace EoaServer.Grain.User;

public class UserGrain : Grain<UserState>, IUserGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<GrainResultDto<UserGrainDto>> Create(UserGrainDto userGrainDto)
    {
        var result = new GrainResultDto<UserGrainDto>();
        if (State.Id != Guid.Empty)
        {
            result.Data = _objectMapper.Map<UserState, UserGrainDto>(State);
            return result;
        }

        State.Id = this.GetPrimaryKey();
        State.UserId = userGrainDto.UserId;
        State.Address = userGrainDto.Address;
        State.CreateTime = DateTime.UtcNow;
        await WriteStateAsync();
        
        result.Data = _objectMapper.Map<UserState, UserGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<UserGrainDto>> UpdateWalletName(string walletName)
    {
        var result = new GrainResultDto<UserGrainDto>();
        if (State.Id == Guid.Empty)
        {
            result.Code = "-1";
            result.Message = "User not exists.";
            return result;
        }

        result.Data = _objectMapper.Map<UserState, UserGrainDto>(State);
        await WriteStateAsync();
        return result;
    }
}