// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ReadModelIndexOperations;

[Collection(SqlServerCollection.Name)]
public class and_the_provider_is_sql_server(SqlServerFixture fixture) : given.an_online_index
{
    protected override ISqlSinkHarness CreateHarness() => new SqlServerSinkHarness { Fixture = fixture };

    protected override Task<bool> IndexExistsInCatalog(ReadModelDbContext context) => context.Database.SqlQuery<int>($"""
        SELECT 1 AS [Value] FROM sys.indexes
        WHERE object_id = OBJECT_ID('online_shadows') AND name = 'chronicle_shx_online_shadows_value'
          AND is_disabled = 0 AND is_hypothetical = 0
        """).AnyAsync();

    Task Because() => BuildAndDrop();

    [Fact] void should_create_a_valid_index() => _existsAfterEnsure.ShouldBeTrue();
    [Fact] void should_preserve_the_valid_index_on_repeated_ensure() => _existsAfterRepeatedEnsure.ShouldBeTrue();
    [Fact] void should_remove_the_index_from_the_catalog() => _existsAfterDrop.ShouldBeFalse();

    [Fact] void should_build_online() => _commands.Sql.Single(sql => sql.StartsWith("CREATE INDEX", StringComparison.Ordinal)).ShouldContain("WITH (ONLINE = ON)");
    [Fact] void should_drop_the_named_shadow_index() => _commands.Sql.Exists(sql => sql == "DROP INDEX [chronicle_shx_online_shadows_value] ON [online_shadows]").ShouldBeTrue();
    [Fact]
    void should_run_outside_a_transaction()
    {
        _commands.Sql.Count.ShouldEqual(2);
        _commands.TransactionalIndex.ShouldBeFalse();
    }
}
