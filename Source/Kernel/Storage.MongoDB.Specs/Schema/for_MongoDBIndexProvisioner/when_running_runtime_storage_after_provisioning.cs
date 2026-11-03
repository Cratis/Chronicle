// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_running_runtime_storage_after_provisioning(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    MongoDBIndexReport _verified;
    long _claims;

    async Task Establish()
    {
        await SaveConstraintDefinitions();
        await _namespace.CreateCollectionAsync("event-log+email+constraint");
        await _namespace.CreateCollectionAsync("event-log+retained-email+values+constraint");
        await _provisioner.Migrate([Store]);
        _verified = await _provisioner.Verify([Store]);
        _commands.Clear();
    }

    async Task Because()
    {
        await given.runtime_index_paths.Exercise(_database, _client.GetDatabase(DatabaseNames.ForEventStoreNamespace(Store, Namespace, _prefix)));
        _claims = await _namespace.GetCollection<BsonDocument>("event-log+email+constraint").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    [Fact] void should_verify_before_runtime_operations() => _verified.IsCurrent.ShouldBeTrue();
    [Fact] void should_not_issue_runtime_index_ddl() => DdlCommands.ShouldEqual(0);
    [Fact] void should_write_through_runtime_storage() => _claims.ShouldEqual(1L);
}
