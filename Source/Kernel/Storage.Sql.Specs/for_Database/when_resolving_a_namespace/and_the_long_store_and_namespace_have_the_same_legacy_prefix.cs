// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_the_long_store_and_namespace_have_the_same_legacy_prefix(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _storeName;
    string _namespaceName;
    string[] _pending;
    int _failedPartitions;

    void Establish() => Store = new string('s', 90);

    async Task Because()
    {
        await using var scope = await Database.Namespace(Store, Namespace);
        _namespaceName = scope.DbContext.Database.GetDbConnection().Database;
        _pending = (await scope.DbContext.Database.GetPendingMigrationsAsync()).ToArray();
        _failedPartitions = await scope.DbContext.FailedPartitions.CountAsync();
        await using var store = await Database.EventStore(Store);
        _storeName = store.DbContext.Database.GetDbConnection().Database;
    }

    [Fact] void should_use_distinct_databases() => _storeName.ShouldNotEqual(_namespaceName);
    [Fact] void should_run_every_namespace_migration() => _pending.ShouldBeEmpty();
    [Fact] void should_have_the_failed_partitions_table() => _failedPartitions.ShouldEqual(0);
}
