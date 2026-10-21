// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_the_future_is_a_join(MongoDBFixture fixture) : given.a_bulk_sink(fixture)
{
    FailedPartition[] _failures;
    BsonDocument _joined;

    async Task Establish()
    {
        await RejectDuplicateCounts();
        await _sink.ApplyChanges(_queuedKey, Changes(1), 1UL);
    }

    async Task Because()
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.Add(new Joined(
            new ExpandoObject(),
            1,
            "count",
            ArrayIndexers.NoIndexers,
            [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("info.name", null, "Joined")])]));
        _failures = (await _sink.ApplyChangesImmediately(new Key(1, ArrayIndexers.NoIndexers), changeset, 2UL, SinkWriteMode.Always)).ToArray();
        _joined = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "existing")).SingleAsync();
    }

    [Fact] void should_return_the_flush_failure_even_without_a_keyed_update() => _failures.Single().EventSourceId.ShouldEqual(_queuedKey);
    [Fact] void should_apply_the_join_before_returning() => _joined["info"]["name"].AsString.ShouldEqual("Joined");
    [Fact] async Task should_not_create_a_phantom_root() => (await _collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty)).ShouldEqual(1L);
}
