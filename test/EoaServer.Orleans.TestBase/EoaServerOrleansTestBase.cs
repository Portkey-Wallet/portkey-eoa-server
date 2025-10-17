using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace EoaServer.Orleans.TestBase;

public abstract class EoaServerOrleansTestBase<TStartupModule> : EoaServerTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public EoaServerOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}