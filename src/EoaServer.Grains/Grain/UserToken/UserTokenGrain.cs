using EoaServer.Commons;
using EoaServer.State.UserToken;
using Volo.Abp.ObjectMapping;

namespace EoaServer.Grain.UserToken;

public class UserTokenGrain : Grain<UserTokenState>, IUserTokenGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserTokenGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<UserTokenGrainDto>> GetAsync()
    {
        if (State.Id == Guid.Empty)
        {
            return new GrainResultDto<UserTokenGrainDto>()
            {
                Code = UserTokenMessage.NotExistMessage
            };
        }

        return new GrainResultDto<UserTokenGrainDto>()
        {
            Code = CommonConstant.SuccessCode,
            Data = _objectMapper.Map<UserTokenState, UserTokenGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<UserTokenGrainDto>> AddAsync(Guid userId, UserTokenGrainDto tokenItem)
    {
        if (State.Id == Guid.Empty)
        {
            State = _objectMapper.Map<UserTokenGrainDto, UserTokenState>(tokenItem);
            State.Id = Guid.NewGuid();
        }
        
        return new GrainResultDto<UserTokenGrainDto>()
        {
            Code = CommonConstant.SuccessCode,
            Data = _objectMapper.Map<UserTokenState, UserTokenGrainDto>(State)
        }; 
    }

    public async Task<GrainResultDto<UserTokenGrainDto>> ChangeDisplayAsync(Guid userId, bool isDisplay, bool isDelete = false)
    {
        if (userId != State.UserId)
        {
            return new GrainResultDto<UserTokenGrainDto>()
            {
                Code = UserTokenMessage.UserNotMatchMessage
            };
        }

        if (State.Token.Symbol == "ELF")
        {
            return new GrainResultDto<UserTokenGrainDto>()
            {
                Code = UserTokenMessage.SymbolCanNotChangeMessage
            };
        }

        State.IsDisplay = isDisplay;
        State.IsDelete = isDelete;
        
        await WriteStateAsync();
        return new GrainResultDto<UserTokenGrainDto>()
        {
            Code = CommonConstant.SuccessCode,
            Data = _objectMapper.Map<UserTokenState, UserTokenGrainDto>(State)
        };
    }
}