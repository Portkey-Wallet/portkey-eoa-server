using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace EoaServer.MongoDb;

[ConnectionStringName("Default")]
public class EoaServerMongoDbContext : AbpMongoDbContext
{
}