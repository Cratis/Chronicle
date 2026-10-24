// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ReadModelIndexOperations;

[Collection(PostgreSqlCollection.Name)]
public class and_the_provider_is_postgresql(PostgreSqlFixture fixture) : given.an_online_index
{
    protected override ISqlSinkHarness CreateHarness() => new PostgreSqlSinkHarness { Fixture = fixture };

    Task Because() => BuildAndDrop();

    [Fact] void should_build_concurrently() => _commands.Sql.Count(sql => sql.StartsWith("CREATE INDEX CONCURRENTLY IF NOT EXISTS \"chronicle_shx_", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_drop_concurrently() => _commands.Sql.Exists(sql => sql.StartsWith("DROP INDEX CONCURRENTLY IF EXISTS \"chronicle_shx_", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_run_outside_a_transaction() => _commands.TransactionalIndex.ShouldBeFalse();
}
