// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel;

[Collection(GlobalCollection.Name)]
public class Test : KernelTest
{
    public Test(KernelFixture fixture) : base(fixture)
    {
        // kernelFixture.EventStore.Database.GetCollection<BsonDocument>("event-log").Find(_ => true).ToList();

        fixture.Cluster.Changes
            .Subscribe(_ => Console.WriteLine("Got cluster change"));

        fixture.SharedEventStore.Changes
            .Subscribe(_ => Console.WriteLine("Got shared change"));

        fixture.EventStore.Changes
            .Subscribe(_ => Console.WriteLine("Got event store change"));

        fixture.SharedEventStore.Changes
             .Where(_ => _.OperationType == ChangeStreamOperationType.Insert)
             .Subscribe(_ => Console.WriteLine("Got insert"));

        var container = new TestClient(fixture, "Basic");
        container.Start().GetAwaiter().GetResult();
    }

    [Fact]
    async Task DoStuff()
    {
        // await Task.Delay(60000);
        Assert.True(true);
    }
}
