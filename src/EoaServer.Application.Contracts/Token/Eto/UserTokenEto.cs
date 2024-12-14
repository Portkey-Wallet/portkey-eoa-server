using System;
using EoaServer.UserToken.Dto;
using Volo.Abp.EventBus;

namespace EoaServer.Token.Eto;

[EventName("UserTokenEto")]
public class UserTokenEto : UserTokenDto
{
}