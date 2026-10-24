// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ReadModelIndexOperations;

[Collection(SqlServerCollection.Name)]
public class and_the_provider_is_sql_server(SqlServerFixture fixture) : given.an_online_index
{
    protected override ISqlSinkHarness CreateHarness() => new SqlServerSinkHarness { Fixture = fixture };

    Task Because() => BuildAndDrop();

    [Fact] void should_build_online() => _commands.Sql.Single(sql => sql.StartsWith("CREATE INDEX", StringComparison.Ordinal)).ShouldContain("WITH (ONLINE = ON)");
    [Fact] void should_drop_the_named_shadow_index() => _commands.Sql.Exists(sql => sql == "DROP INDEX [chronicle_shx_online_shadows_value] ON [online_shadows]").ShouldBeTrue();
    [Fact] void should_run_outside_a_transaction() => _commands.TransactionalIndex.ShouldBeFalse();
}
