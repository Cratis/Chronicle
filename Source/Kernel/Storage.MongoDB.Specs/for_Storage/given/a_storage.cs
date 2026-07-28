// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_Storage.given;

public class a_storage : Specification
{
    protected IMongoCollection<EventStore> _collection;
    protected ControllableDatabase _database;
    protected Storage _storage;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<EventStore>>();
        _database = new ControllableDatabase(_collection);
        _storage = new Storage(
            _database,
            Substitute.For<Chronicle.Json.IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            new KnownInstancesOf<ISinkFactory>([]),
            Substitute.For<IJobTypes>(),
            Options.Create(new ChronicleOptions()),
            NullLoggerFactory.Instance);
    }

    void Destroy() => _database.Dispose();
}
