// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Specs;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage.given;

public class an_event_store_storage : Specification
{
    protected static readonly EventStoreName _eventStore = "SomeEventStore";
    protected ControllableEventStoreDatabase _eventStoreDatabase;
    protected EventStoreStorage _storage;

    void Establish()
    {
        _eventStoreDatabase = new ControllableEventStoreDatabase();
        _storage = new EventStoreStorage(
            _eventStore,
            _eventStoreDatabase,
            Substitute.For<Chronicle.Json.IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            @namespace => new Chronicle.Storage.Sinks.Sinks(_eventStore, @namespace, new KnownInstancesOf<ISinkFactory>([])),
            JobsStorageForSpecs.Create(new MongoClient("mongodb://localhost:27017")),
            Options.Create(new ChronicleOptions()),
            NullLoggerFactory.Instance);
    }

    void Destroy() => _eventStoreDatabase.Dispose();
}
