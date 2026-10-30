// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MongoDBMaterializationBindingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reserving_over_an_occupied_destination(ReplicaSetMongoDBFixture fixture) : given.a_mongo_binding(fixture)
{
    GenerationReservation _result;
    Task Establish() => Database.GetCollection<BsonDocument>("models-g2").InsertOneAsync(new BsonDocument("foreign", true));
    async Task Because() => _result = await _storage.ReserveNext(_store, _key, "revision", DateTimeOffset.UnixEpoch);
    [Fact] void should_refuse() => _result.Result.ShouldEqual(GenerationReservationResult.DestinationOccupied);
    [Fact] async Task should_not_advance_the_counter() => (await _storage.Get(_store, _key))!.Next.Value.ShouldEqual(1u);
}
