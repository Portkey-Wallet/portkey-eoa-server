using Orleans.TestingHost;

namespace EoaServer.Grain.Tests;

public class EoaServerGrainTestBase :EoaServerTestBase<EoaServerGrainTestModule>
{
    protected readonly TestCluster Cluster;

    public EoaServerGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;

    }
}