// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

[Collection(PostgreSqlCollection.Name)]
public class and_storage_may_not_create_the_placed_database(PostgreSqlFixture fixture) : given.a_postgresql_database_with_connection_resolution(fixture)
{
    Exception? _error;

    void Establish() => Resolver.Answer = request => request.Scope[StorageConnectionRequests.Kind] == "namespace"
        ? new ResolvedConnection(Database: $"{DatabaseName}_missing")
        : ResolvedConnection.Unchanged;

    async Task Because()
    {
        var database = ResolvingDatabase(_ => _.Connection.UseCustom<given.RecordingConnectionResolver>(), createIfMissing: false);
        try
        {
            await using var scope = await database.Namespace(Store, Namespace);
        }
        catch (DatabaseDoesNotExist ex)
        {
            _error = ex;
        }
    }

    [Fact] void should_fail_because_the_database_does_not_exist() => _error.ShouldNotBeNull();
    [Fact] void should_name_the_database() => _error!.Message.ShouldContain($"{DatabaseName}_missing");
}
