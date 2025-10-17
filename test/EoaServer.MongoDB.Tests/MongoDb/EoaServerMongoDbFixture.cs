using System;

namespace EoaServer.MongoDb;

public class EoaServerMongoDbFixture : IDisposable
{
    // private static readonly MongoDbRunner MongoDbRunner;
    // public static readonly string ConnectionString;
    //
    // static IMMongoDbFixture()
    // {
    //     MongoDbRunner = MongoDbRunner.Start(singleNodeReplSet: true, singleNodeReplSetWaitTimeout: 10);
    //     ConnectionString = MongoDbRunner.ConnectionString;
    // }

    public void Dispose()
    {
        //MongoDbRunner?.Dispose();
    }
}
