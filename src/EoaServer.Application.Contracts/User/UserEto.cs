using System;

namespace EoaServer.User;

public class UserEto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Address { get; set; }
    public string WalletName { get; set; }
    public DateTime CreateTime { get; set; }
}