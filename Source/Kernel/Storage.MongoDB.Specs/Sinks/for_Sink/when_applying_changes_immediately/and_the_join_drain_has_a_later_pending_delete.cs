// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_the_join_drain_has_a_later_pending_delete(MongoDBFixture fixture) : given.a_controlled_mongo_sink(fixture)
{
    readonly Key _pendingDeleteKey = new("pending-delete", ArrayIndexers.NoIndexers);
    Task<IEnumerable<FailedPartition>> _queuedAfterFirst;
    Task<IEnumerable<FailedPartition>> _queuedDelete;
    ExpandoObject? _logicalState;
    bool _deleteWasStillQueued;

    async Task Establish()
    {
        await _collection.InsertOneAsync(new BsonDocument { { "_id", "pending-delete" }, { "count", 4 } });
        await _sink.ApplyChanges(_queuedKey, Changes(2), 1UL);
        _afterBatch = QueueAfterBatch;
    }

    async Task Because()
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.Add(new Joined(
            new ExpandoObject(),
            2,
            "count",
            ArrayIndexers.NoIndexers,
            [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("info.name", null, "Joined")])]));
        await _sink.ApplyChangesImmediately(new Key(2, ArrayIndexers.NoIndexers), changeset, 3UL, SinkWriteMode.Always);
        await _queuedAfterFirst;
        await _queuedDelete;
        _logicalState = await _sink.FindOrDefault(_pendingDeleteKey);
        _deleteWasStillQueued = await Document(_pendingDeleteKey) is not null;
        await _sink.EndBulk();
    }

    void QueueAfterBatch(int batchNumber)
    {
        if (batchNumber == 1)
        {
            _queuedAfterFirst = _sink.ApplyChanges(_futureKey, Changes(3), 2UL);
        }
        else if (batchNumber == 2)
        {
            _queuedDelete = _sink.ApplyChanges(_pendingDeleteKey, Removal(), 4UL);
        }
    }

    [Fact] void should_preserve_the_logical_pending_delete() => _logicalState.ShouldBeNull();
    [Fact] void should_leave_the_new_delete_queued_until_end_bulk() => _deleteWasStillQueued.ShouldBeTrue();
    [Fact] async Task should_flush_the_pending_delete_at_end_bulk() => (await Document(_pendingDeleteKey)).ShouldBeNull();
}
