// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ClosedStreamsConstraintStorage;

[Collection(MongoDBCollection.Name)]
public class when_reading_a_legacy_document(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    ClosedStreamsConstraintStorage _storage;
    IEnumerable<ClosedStream> _rows;

    async Task Establish()
    {
        _storage = new(_database, EventSequenceId.Log);
        await _rawDatabase.GetCollection<BsonDocument>($"{EventSequenceId.Log}+closed_streams").InsertOneAsync(new BsonDocument
        {
            { "streamType", "transactions" },
            { "streamId", "month" }
        });
    }

    async Task Because() => _rows = await _storage.GetCovering(new(EventStreamType: "transactions", EventStreamId: "month"), await _storage.GetDimensionsInUse());

    [Fact] void should_read_one_legacy_scope() => _rows.Count().ShouldEqual(1);
    [Fact] void should_read_manual_owner() => _rows.Single().Owner.ShouldEqual(ClosedStreamOwner.Manual);
    [Fact] void should_read_unavailable_sequence_number() => _rows.Single().SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_read_no_timestamp() => _rows.Single().ClosedAt.ShouldBeNull();
    [Fact] async Task should_include_legacy_dimensions() => (await _storage.GetDimensionsInUse()).ShouldContainOnly(ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId);
}
