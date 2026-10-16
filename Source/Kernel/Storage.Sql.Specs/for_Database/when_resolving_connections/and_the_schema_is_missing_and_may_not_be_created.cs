// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

[Collection(PostgreSqlCollection.Name)]
public class and_the_schema_is_missing_and_may_not_be_created(PostgreSqlFixture fixture) : given.a_postgresql_database_with_connection_resolution(fixture)
{
    Exception? _error;

    async Task Because()
    {
        var database = CreateDatabase(new() { Type = StorageType.PostgreSql, ConnectionDetails = ConnectionString, Schema = "missing", CreateIfMissing = false }, Services);
        try
        {
            await using var scope = await database.Cluster();
        }
        catch (SchemaDoesNotExist ex)
        {
            _error = ex;
        }
    }

    [Fact] void should_fail_because_the_schema_does_not_exist() => _error.ShouldNotBeNull();
    [Fact] void should_name_the_schema() => _error!.Message.ShouldContain("missing");
}
