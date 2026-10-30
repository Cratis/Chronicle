// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_verifying_fixed_startup_scopes(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    MongoDBIndexReport _startup;
    MongoDBIndexReport _cli;
    (string Database, string Collection)[] _startupQueries;
    int _startupDdl;

    async Task Establish()
    {
        await _database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores).InsertOneAsync(new(Store));
        await _database.GetEventStoreDatabase(EventStoreName.System).GetCollection<BsonDocument>(WellKnownCollectionNames.Namespaces).InsertOneAsync(new("_id", "another-tenant"));
    }

    async Task Because()
    {
        _commands.Clear();
        _queries.Clear();
        _startup = await _provisioner.VerifyStartup();
        _startupQueries = _queries.ToArray();
        _startupDdl = DdlCommands;
        _cli = await _provisioner.Verify();
    }

    [Fact] void should_check_only_the_fixed_startup_scopes() => _startup.Scopes.Select(scope => scope.Scope).Order(StringComparer.Ordinal).ToArray().ShouldEqual(
        new[] { "event-store:System", "namespace:System/Default", "jobs:System/Default", "global-encryption" }.Order(StringComparer.Ordinal).ToArray());
    [Fact] void should_not_discover_event_stores_or_namespaces() => _startupQueries.Any(query => IsAnyOf(query.Collection, WellKnownCollectionNames.EventStores, WellKnownCollectionNames.Namespaces)).ShouldBeFalse();
    [Fact] void should_not_query_another_store() => _startupQueries.Any(query => query.Database == DatabaseNames.ForEventStore(Store, _prefix) || query.Database == DatabaseNames.ForEventStoreNamespace(Store, Namespace, _prefix)).ShouldBeFalse();
    [Fact] void should_not_issue_startup_ddl() => _startupDdl.ShouldEqual(0);
    [Fact] void should_keep_cli_event_store_discovery() => _cli.Scopes.Any(scope => scope.Scope == $"namespace:{Store}/{Namespace}").ShouldBeTrue();
    [Fact] void should_keep_cli_namespace_discovery() => _cli.Scopes.Any(scope => scope.Scope == "namespace:System/another-tenant").ShouldBeTrue();
}
