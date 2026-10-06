// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_newer_delete_is_queued_for_the_same_key : for_Sink.given.a_sink_with_gated_bulk_writes
{
    async Task Establish()
    {
        await _sink.BeginBulk();
        var removal = Changes(0);
        removal.Remove();
        await _sink.ApplyChanges(_firstKey, removal, 1UL);
    }

    async Task Because()
    {
        var join = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        join.Add(new Joined(new ExpandoObject(), "other", "id", ArrayIndexers.NoIndexers, []));
        var flushing = _sink.ApplyChanges(_secondKey, join, 2UL);
        await _firstFlushStarted.Task;
        var removal = Changes(0);
        removal.Remove();
        await _sink.ApplyChanges(_firstKey, removal, 3UL);
        _releaseFirstFlush.SetResult();
        await flushing;
        await _sink.FindOrDefault(_firstKey);
        await _sink.EndBulk();
    }

    [Fact] void should_keep_the_newer_delete_mark_until_its_own_flush() => _collection.DidNotReceive().FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>());
    [Fact] void should_flush_both_deletes() => _batches.SelectMany(batch => batch).OfType<DeleteOneModel<BsonDocument>>().Count().ShouldEqual(2);
}
