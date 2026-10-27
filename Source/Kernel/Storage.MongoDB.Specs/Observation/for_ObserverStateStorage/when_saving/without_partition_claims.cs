// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class without_partition_claims : Specification
{
    ObserverStateStorage _storage;
    BsonDocument _set;

    void Establish()
    {
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        var collection = Substitute.For<IMongoCollection<ObserverState>>();
        database.GetObserverStateCollection().Returns(collection);
        collection.UpdateOneAsync(
            Arg.Any<FilterDefinition<ObserverState>>(),
            Arg.Do<UpdateDefinition<ObserverState>>(update =>
            {
                var registry = BsonSerializer.SerializerRegistry;
                _set = update.Render(new RenderArgs<ObserverState>(registry.GetSerializer<ObserverState>(), registry))["$set"].AsBsonDocument;
            }),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>()).Returns(new UpdateResult.Acknowledged(1, 1, null));
        _storage = new(database);
    }

    async Task Because() => await _storage.Save(KernelObserverState.Empty with { Identifier = "observer" }, Concepts.EventSequences.EventSequenceId.Log);

    [Fact] void should_write_null_partition_claims() => ValueIn(nameof(ObserverState.PartitionClaims)).ShouldEqual(BsonNull.Value);
    [Fact] void should_write_null_observer_wide_claim() => ValueIn(nameof(ObserverState.ObserverWideClaim)).ShouldEqual(BsonNull.Value);
    [Fact] void should_write_null_ownership_revision() => ValueIn(nameof(ObserverState.OwnershipRevision)).ShouldEqual(BsonNull.Value);

    BsonValue ValueIn(string property) => _set[BsonClassMap.LookupClassMap(typeof(ObserverState)).GetMemberMap(property).ElementName];
}
