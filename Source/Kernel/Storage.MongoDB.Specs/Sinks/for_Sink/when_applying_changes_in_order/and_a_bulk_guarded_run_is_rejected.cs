// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order;

public class and_a_bulk_guarded_run_is_rejected : given.a_bulk_sink_recording_writes
{
    readonly List<int> _directAttempts = [];
    int _rejected;

    async Task Establish()
    {
        _documents[_key]["value"] = 100;
        _documents[_key][WellKnownProperties.LastHandledEventSequenceNumber] = 100L;
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => ApplyDirect(call.Arg<FilterDefinition<BsonDocument>>(), call.Arg<UpdateDefinition<BsonDocument>>()));
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => ApplyBulk(call.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0).ToArray()));
        await _sink.BeginBulk();
    }

    async Task Because()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([Direct(10), Join(20), Direct(30)]);
        changeset.HasJoined().Returns(true);
        await _sink.ApplyChanges(RootKey, changeset, (EventSequenceNumber)42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);
        await _sink.EndBulk();
    }

    [Fact] void should_reject_the_stale_first_run() => _rejected.ShouldEqual(1);
    [Fact] void should_not_attempt_the_later_direct_run() => _directAttempts.ShouldEqual([10]);
    [Fact] void should_not_continue_to_the_join() => _collection.DidNotReceive().UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_preserve_the_newer_stored_value() => _documents[_key]["value"].AsInt32.ShouldEqual(100);

    UpdateResult ApplyDirect(FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update)
    {
        var args = new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
        var value = update.Render(args)["$set"]["value"].AsInt32;
        _directAttempts.Add(value);

        // The stored watermark is 100; the incoming event's below-42 predicate matches nothing.
        if (filter.Render(args).Contains("$or"))
        {
            _rejected++;
            return new UpdateResult.Acknowledged(0, 0, null);
        }

        _documents[_key]["value"] = value;
        return new UpdateResult.Acknowledged(1, 1, null);
    }

    BulkWriteResult<BsonDocument> ApplyBulk(WriteModel<BsonDocument>[] operations)
    {
        var matched = operations.Cast<UpdateOneModel<BsonDocument>>().Sum(operation => ApplyDirect(operation.Filter, operation.Update).MatchedCount);
        return new BulkWriteResult<BsonDocument>.Acknowledged(operations.Length, matched, 0, 0, matched, operations, []);
    }
}
