// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_the_shared_legacy_database_has_only_jobs_history(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    const string JobsHistoryOnly =
        "CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" varchar(150) PRIMARY KEY, \"ProductVersion\" varchar(32) NOT NULL); " +
        "INSERT INTO \"__EFMigrationsHistory\" VALUES ('Cratis-Orleans-Jobs-v1_0_0', '10.0.0')";

    Exception _error;

    async Task Establish()
    {
        Store = new string('s', 90);
        var legacy = LogicalStoreName[..63];
        await SeedStore(Store);
        await CreateLegacyDatabase(legacy);
        await Execute(ConnectionFor(legacy), JobsHistoryOnly);
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await using var scope = await Database.Namespace(Store, Namespace);
    });

    [Fact] void should_require_an_operator_binding() => _error.ShouldBeOfExactType<DatabaseOwnershipUnresolved>();
}
