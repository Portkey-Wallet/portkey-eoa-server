namespace EoaServer.Entities.Redis;

public class TestCacheInfo
{
    public string UserId { get; set; }
    public string AppId { get; set; }

    public string GetKey()
    {
        return UserId;
    }
}