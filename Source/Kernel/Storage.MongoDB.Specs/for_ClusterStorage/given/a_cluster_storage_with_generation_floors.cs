// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_ClusterStorage.given;

public class a_cluster_storage_with_generation_floors(ReplicaSetMongoDBFixture fixture) : Generations.for_GenerationAdmission.given.a_provider(fixture)
{
    protected readonly EventStoreName _unregistered = new("billing");
    protected ClusterStorage _clusterStorage;
    protected Chronicle.Storage.Storage _sharedStorage;
    protected string[] _before;

    async Task Establish()
    {
        var factories = Substitute.For<IInstancesOf<ISinkFactory>>();
        factories.GetEnumerator().Returns(_ => Enumerable.Empty<ISinkFactory>().GetEnumerator());
        _clusterStorage = new(_database, Substitute.For<Json.IExpandoObjectConverter>(), new JsonSerializerOptions(), Substitute.For<Cratis.Orleans.Storage.IJobsStorage>(), Options.Create(new ChronicleOptions()), NullLoggerFactory.Instance);
        _sharedStorage = new(_clusterStorage, _storage.System, factories);
        await Advance(2);
        await WriteFloor(_unregistered);
        await WriteFloor(EventStoreName.System);
        _before = await Databases();
    }

    async Task WriteFloor(EventStoreName store)
    {
        var floor = new GenerationFloor(new(2), new(2), [GenerationCapability.FloorAdmission], new(Guid.NewGuid()), 0, default, "", "");
        var provider = new Generations.GenerationFloorStorage(_database.GetEventStoreDatabase(store).GetCollection<BsonDocument>("generation-floor"));
        await provider.Advance(new(floor, 0, "spec", "floor", new(true, "uniform")));
    }

    protected async Task<EventStoreName[]> RegisteredStores() =>
        (await _registry.Find(FilterDefinition<EventStore>.Empty).ToListAsync()).Select(_ => _.Name).ToArray();
}
