// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel;

[Collection(GlobalCollection.Name)]
public class Test : IClassFixture<KernelFixture>
{
    readonly GlobalFixture _globalFixture;
    readonly KernelFixture _kernelFixture;

    public Test(
        KernelFixture kernelFixture,
        GlobalFixture globalFixture)
    {
        _globalFixture = globalFixture;
        _kernelFixture = kernelFixture;

        // _globalFixture.EventStore.Database.GetCollection<BsonDocument>("event-log").Find(_ => true).ToList();

        _globalFixture.Cluster.Changes
            .Subscribe(_ => Console.WriteLine("Got cluster change"));

        _globalFixture.SharedEventStore.Changes
            .Subscribe(_ => Console.WriteLine("Got shared change"));

        _globalFixture.EventStore.Changes
            .Subscribe(_ => Console.WriteLine("Got event store change"));

        _globalFixture.SharedEventStore.Changes
             .Where(_ => _.OperationType == ChangeStreamOperationType.Insert)
             .Subscribe(_ => Console.WriteLine("Got insert"));

        var container = new TestClient("Basic", "Basic");
        container.Start().GetAwaiter().GetResult();
    }

    [Fact]
    async Task DoStuff()
    {
        await Task.Delay(60000);
        Assert.True(true);
    }
}
