// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_discovering_existing_collections(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    MongoDBIndexReport _report;
    string[] _collections;

    async Task Establish()
    {
        await SaveConstraintDefinitions();
        await _database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores).InsertOneAsync(new(Store));
        await _namespace.GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).InsertOneAsync(new("_id", "custom"));
        await _namespace.CreateCollectionAsync("custom+email+constraint");
        await _namespace.CreateCollectionAsync("custom+retained-email+values+constraint");
        await _namespace.CreateCollectionAsync("unrelated");
    }

    async Task Because()
    {
        _report = await _provisioner.Migrate();
        using var cursor = await _namespace.ListCollectionNamesAsync();
        _collections = (await cursor.ToListAsync()).ToArray();
    }

    [Fact] void should_discover_registered_event_stores() => _report.Scopes.Any(scope => scope.Scope == $"namespace:{Store}/{Namespace}").ShouldBeTrue();
    [Fact] async Task should_provision_custom_event_sequences() => (await Indexes(_namespace, "custom")).Any(index => index["name"] == "publication_identity").ShouldBeTrue();
    [Fact] async Task should_provision_custom_closed_streams() => (await Indexes(_namespace, "custom+closed_streams")).Any(index => index["name"] == "scope_owner").ShouldBeTrue();
    [Fact] async Task should_provision_per_source_claims() => (await Indexes(_namespace, "custom+email+constraint")).Any(index => index["name"] == "value").ShouldBeTrue();
    [Fact] async Task should_provision_per_value_claims() => (await Indexes(_namespace, "custom+retained-email+values+constraint")).Any(index => index["name"] == "owner").ShouldBeTrue();
    [Fact] async Task should_leave_unrelated_collections_alone() => (await Indexes(_namespace, "unrelated")).Count.ShouldEqual(1);
    [Fact] void should_not_invent_constraint_collections() => _collections.Count(name => name.EndsWith("+constraint", StringComparison.Ordinal)).ShouldEqual(2);
}
