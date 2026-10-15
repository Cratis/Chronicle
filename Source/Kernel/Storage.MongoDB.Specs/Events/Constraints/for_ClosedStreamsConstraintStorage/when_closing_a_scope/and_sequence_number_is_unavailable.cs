// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ClosedStreamsConstraintStorage.when_closing_a_scope;

[Collection(MongoDBCollection.Name)]
public class and_sequence_number_is_unavailable(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    ClosedStreamsConstraintStorage _storage;
    readonly ClosedStream _closure = new(new(EventSourceId: "source"), ClosedStreamOwner.Manual, EventSequenceNumber.Unavailable, null);

    void Establish() => _storage = new(_database, EventSequenceId.Log);

    async Task Because() => await _storage.Close(_closure);

    [Fact] async Task should_read_the_unavailable_sequence_number() => (await _storage.GetAll()).ShouldContainOnly(_closure);
    [Fact] async Task should_store_the_sequence_number_as_decimal128() => (await _rawDatabase.GetCollection<BsonDocument>($"{EventSequenceId.Log}+closed_streams").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync())["sequenceNumber"].BsonType.ShouldEqual(BsonType.Decimal128);
}
