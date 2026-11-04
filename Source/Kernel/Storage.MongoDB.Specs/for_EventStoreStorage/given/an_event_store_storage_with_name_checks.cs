// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.MongoDB.Specs;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Naming.for_ResourceNameReservations;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage.given;

public class an_event_store_storage_with_name_checks : Specification
{
    protected static readonly EventStoreName _eventStore = "SomeEventStore";
    protected ControllableEventStoreDatabase _eventStoreDatabase;
    protected ResourceNameSnapshot _snapshot;
    protected EventStoreStorage _storage;

    void Establish()
    {
        _eventStoreDatabase = new ControllableEventStoreDatabase();

        // The registered namespaces are read when a namespace is opened for the first time; there are none.
        var cursor = Substitute.For<IAsyncCursor<MongoDBNamespace>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
        _eventStoreDatabase.GetCollection<MongoDBNamespace>()
            .FindAsync(Arg.Any<FilterDefinition<MongoDBNamespace>>(), Arg.Any<FindOptions<MongoDBNamespace, MongoDBNamespace>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));

        _snapshot = ResourceNameSnapshot.NotReady();
        _snapshot.Publish([], []);
        var gate = new ResourceNameGate(_snapshot, new FakeReservations(), ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);
        _storage = new EventStoreStorage(
            _eventStore,
            _eventStoreDatabase,
            Substitute.For<Chronicle.Json.IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            @namespace => new Chronicle.Storage.Sinks.Sinks(_eventStore, @namespace, new KnownInstancesOf<ISinkFactory>([])),
            JobsStorageForSpecs.Create(new MongoClient("mongodb://localhost:27017")),
            Options.Create(new ChronicleOptions()),
            NullLoggerFactory.Instance,
            new NamespaceOpeningGuard(gate, () => Task.FromResult<IEnumerable<string>>([_eventStore.Value])));
    }

    void Destroy() => _eventStoreDatabase.Dispose();
}
