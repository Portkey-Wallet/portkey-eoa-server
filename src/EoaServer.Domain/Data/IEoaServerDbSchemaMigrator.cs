using System.Threading.Tasks;

namespace EoaServer.Data;

public interface IEoaServerDbSchemaMigrator
{
    Task MigrateAsync();
}
