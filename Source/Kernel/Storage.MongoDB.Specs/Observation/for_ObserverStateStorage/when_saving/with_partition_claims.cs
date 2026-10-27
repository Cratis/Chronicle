// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class with_partition_claims : Specification
{
    ObserverStateStorage _storage;
    KernelObserverState _state;
    BsonDocument _set;
    BsonDocument _expected;

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
        _state = new()
        {
            Identifier = "observer",
            PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, null, new(3), 42, [])],
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
            OwnershipRevision = new(7)
        };
        _expected = _state.ToMongoDB().ToBsonDocument();
    }

    async Task Because() => await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);

    [Fact] void should_set_partition_claims() => ValueIn(_set, nameof(ObserverState.PartitionClaims)).ShouldEqual(ValueIn(_expected, nameof(ObserverState.PartitionClaims)));
    [Fact] void should_set_the_observer_wide_claim() => ValueIn(_set, nameof(ObserverState.ObserverWideClaim)).ShouldEqual(ValueIn(_expected, nameof(ObserverState.ObserverWideClaim)));
    [Fact] void should_set_the_ownership_revision() => ValueIn(_set, nameof(ObserverState.OwnershipRevision)).ShouldEqual(ValueIn(_expected, nameof(ObserverState.OwnershipRevision)));

    static BsonValue ValueIn(BsonDocument document, string property) => document[BsonClassMap.LookupClassMap(typeof(ObserverState)).GetMemberMap(property).ElementName];
}
