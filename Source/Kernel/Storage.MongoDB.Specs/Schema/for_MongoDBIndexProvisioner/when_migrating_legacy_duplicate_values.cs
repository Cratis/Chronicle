// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_migrating_legacy_duplicate_values(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    BsonDocument _index;
    MongoDBIndexReport _report;

    async Task Establish()
    {
        await SaveConstraintDefinitions();
        await _namespace.GetCollection<UniqueConstraintIndex>("event-log+email+constraint").InsertManyAsync(
        [
            new(EventSourceId.New(), "shared", 0),
            new(EventSourceId.New(), "shared", 1)
        ]);
    }

    async Task Because()
    {
        _report = await _provisioner.Migrate([Store]);
        _index = (await Indexes(_namespace, "event-log+email+constraint")).Single(index => index["name"] == "value");
    }

    [Fact] void should_preserve_the_non_unique_fallback() => _index.GetValue("unique", false).AsBoolean.ShouldBeFalse();
    [Fact] void should_report_the_fallback_as_provisioned() => _report.IsCurrent.ShouldBeTrue();
}
