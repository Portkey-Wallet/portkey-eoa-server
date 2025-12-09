using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace EoaServer.Data;

public class NullEoaServerDbSchemaMigrator : IEoaServerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
