// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ReadModelIndexOperations;

[Collection(PostgreSqlCollection.Name)]
public class and_the_provider_is_postgresql(PostgreSqlFixture fixture) : given.an_online_index
{
    protected override ISqlSinkHarness CreateHarness() => new PostgreSqlSinkHarness { Fixture = fixture };

    protected override Task<bool> IndexExistsInCatalog(ReadModelDbContext context) => context.Database.SqlQuery<int>($"""
        SELECT 1 AS "Value" FROM pg_indexes i
        JOIN pg_namespace n ON n.nspname = i.schemaname
        JOIN pg_class c ON c.relnamespace = n.oid AND c.relname = i.indexname
        JOIN pg_index x ON x.indexrelid = c.oid
        WHERE i.schemaname = current_schema() AND i.tablename = 'online_shadows'
          AND i.indexname = 'chronicle_shx_online_shadows_value' AND x.indisvalid AND x.indisready
        """).AnyAsync();

    Task Because() => BuildAndDrop();

    [Fact] void should_create_a_valid_index() => _existsAfterEnsure.ShouldBeTrue();
    [Fact] void should_preserve_the_valid_index_on_repeated_ensure() => _existsAfterRepeatedEnsure.ShouldBeTrue();
    [Fact] void should_remove_the_index_from_the_catalog() => _existsAfterDrop.ShouldBeFalse();

    [Fact]
    void should_build_concurrently() => _commands.Sql.Where(sql => sql.StartsWith("CREATE INDEX", StringComparison.Ordinal)).ShouldEqual([
        "CREATE INDEX CONCURRENTLY IF NOT EXISTS chronicle_shx_online_shadows_value ON online_shadows (value)",
        "CREATE INDEX CONCURRENTLY IF NOT EXISTS chronicle_shx_online_shadows_value ON online_shadows (value)"
    ]);

    [Fact] void should_drop_concurrently() => _commands.Sql.Single(sql => sql.StartsWith("DROP INDEX", StringComparison.Ordinal)).ShouldEqual("DROP INDEX CONCURRENTLY IF EXISTS chronicle_shx_online_shadows_value");

    [Fact]
    void should_run_outside_a_transaction()
    {
        _commands.Sql.Count.ShouldEqual(3);
        _commands.TransactionalIndex.ShouldBeFalse();
    }
}
