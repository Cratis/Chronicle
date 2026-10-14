// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_the_store_database_name_is_exactly_at_the_limit(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _storeName;
    string _namespaceName;

    void Establish() => Store = new string('s', 63 - new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString).Database!.Length - 4);

    async Task Because()
    {
        await using var ns = await Database.Namespace(Store, Namespace);
        _namespaceName = ns.DbContext.Database.GetDbConnection().Database;
        await using var store = await Database.EventStore(Store);
        _storeName = store.DbContext.Database.GetDbConnection().Database;
    }

    [Fact] void should_keep_the_unchanged_store_database() => _storeName.ShouldEqual(LogicalStoreName);
    [Fact] void should_separate_the_namespace_reached_first() => _namespaceName.ShouldNotEqual(_storeName);
}
