// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if false

using System.Reactive.Linq;
using System.Reactive.Subjects;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel;

[Collection(GlobalCollection.Name)]
public class Test : KernelTest
{
    int _eventLogCount;

    public Test(KernelFixture fixture) : base(fixture)
    {
        fixture.Cluster.Changes
            .Subscribe(_ => Console.WriteLine("Got cluster change"));

        fixture.SharedEventStore.Changes
            .Subscribe(_ => Console.WriteLine("Got shared change"));

        fixture.EventStore.Changes
            .Subscribe(_ => Console.WriteLine($"Got event store change ({_.CollectionNamespace.CollectionName}) - {_.OperationType}"));

        fixture.SharedEventStore.Changes
             .Where(_ => _.OperationType == ChangeStreamOperationType.Insert)
             .Subscribe(_ => Console.WriteLine($"Got insert into {_.CollectionNamespace.CollectionName}"));

        var eventLogInserts = fixture.EventStore.Changes
            .Where(_ =>
                _.OperationType == ChangeStreamOperationType.Insert &&
                _.CollectionNamespace.CollectionName == "event-log");

        eventLogInserts.Subscribe(_ => Console.WriteLine("Got an insert into the event log"));
        var eventLogCount = eventLogInserts.Count();
        eventLogCount.Subscribe(_ =>
        {
            _eventLogCount = _;
            Console.WriteLine($"Got event log count: {_}");
        });

        var container = new TestClient(fixture, "Basic");
        container.Start().GetAwaiter().GetResult();
    }

    [Fact]
    async Task should_have_one_event_inserted_in_event_log()
    {
        await Task.Delay(1000);

        KernelFixture.EventStore.Complete();

        _eventLogCount.ShouldEqual(1);

        var count = await KernelFixture.EventStore.Database.GetCollection<BsonDocument>("event-log").CountDocumentsAsync(_ => true);
        count.ShouldEqual(1);
    }
}

#endif
