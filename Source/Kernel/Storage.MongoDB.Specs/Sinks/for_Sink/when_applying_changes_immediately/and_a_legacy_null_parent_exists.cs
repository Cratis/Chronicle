// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_a_legacy_null_parent_exists(MongoDBFixture fixture) : given.a_bulk_sink(fixture)
{
    BsonDocument? _written;
    FailedPartition[] _failures;

    async Task Establish() => await _collection.InsertOneAsync(new BsonDocument { { "_id", "future" }, { "info", BsonNull.Value } });

    async Task Because()
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, (await _sink.FindOrDefault(_futureKey))!);
        changeset.SetProperties([PropertyMappers.FromEventValueProvider("info.name", _ => "Recreated")], ArrayIndexers.NoIndexers);
        _failures = (await _sink.ApplyChangesImmediately(_futureKey, changeset, 1UL, SinkWriteMode.Always)).ToArray();
        _written = await Document(_futureKey);
    }

    [Fact] void should_repair_the_null_parent_before_the_immediate_update() => _written!["info"]["name"].AsString.ShouldEqual("Recreated");
    [Fact] void should_not_report_write_failures() => _failures.ShouldBeEmpty();
    [Fact] async Task should_leave_nothing_buffered() => (await _sink.EndBulk()).ShouldBeEmpty();
}
