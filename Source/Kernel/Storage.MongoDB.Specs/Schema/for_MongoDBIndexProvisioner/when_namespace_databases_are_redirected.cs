// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_namespace_databases_are_redirected(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    MongoDBIndexReport _report;

    void Establish()
    {
        var connections = Substitute.For<IMongoDBStorageConnections>();
        connections.GetDatabase(Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => _client.GetDatabase(call.ArgAt<StorageConnectionKind>(0) == StorageConnectionKind.Namespace
                ? $"{_prefix}redirected"
                : call.ArgAt<string>(1)));
        _database = new(_clients, _mongoOptions, Substitute.For<ICustomSerializers>(), _storageOptions, connections);
        _provisioner = new(_database, _clients, _mongoOptions, _storageOptions, NullLoggerFactory.Instance);
    }

    async Task Because() => _report = await _provisioner.Migrate([Store]);

    [Fact] void should_route_namespace_indexes() => _report.Scopes.Single(scope => scope.Scope == $"namespace:{Store}/{Namespace}").Database.ShouldEqual($"{_prefix}redirected");
    [Fact] void should_not_redirect_jobs() => _report.Scopes.Single(scope => scope.Scope == $"jobs:{Store}/{Namespace}").Database.ShouldEqual(DatabaseNames.ForEventStoreNamespace(Store, Namespace, _prefix));
    [Fact] async Task should_provision_the_actual_jobs_database() => (await Indexes(_client.GetDatabase(DatabaseNames.ForEventStoreNamespace(Store, Namespace, _prefix)), WellKnownCollectionNames.Jobs)).Any(index => index["name"] == "status").ShouldBeTrue();
    [Fact] async Task should_not_put_jobs_in_the_redirected_database() => (await MongoDBIndexCatalogue.ForJobs(_client.GetDatabase($"{_prefix}redirected"))[0].Existing()).Contains("status").ShouldBeFalse();
}
