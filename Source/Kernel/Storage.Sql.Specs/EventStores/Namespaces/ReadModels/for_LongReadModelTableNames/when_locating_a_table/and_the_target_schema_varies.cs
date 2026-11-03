// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelTableNames.when_locating_a_table;

public class and_the_target_schema_varies : Specification
{
    string _empty;
    string _named;
    string _long;
    string _sqlServerLong;
    string _sqlite;

    void Because()
    {
        _empty = LongReadModelTableNames.SchemaFor(DatabaseType.PostgreSql, string.Empty);
        _named = LongReadModelTableNames.SchemaFor(DatabaseType.PostgreSql, "tenant_a");
        _long = LongReadModelTableNames.SchemaFor(DatabaseType.PostgreSql, new string('t', 60));
        _sqlServerLong = LongReadModelTableNames.SchemaFor(DatabaseType.SqlServer, new string('t', 125));
        _sqlite = LongReadModelTableNames.SchemaFor(DatabaseType.Sqlite, "tenant_a");
    }

    [Fact] void should_use_the_default_schema_for_an_empty_target() => _empty.ShouldEqual("chronicle_long_names");
    [Fact] void should_derive_the_schema_from_a_named_target() => _named.ShouldEqual("tenant_a_long_names");
    [Fact] void should_hash_an_over_limit_postgresql_schema() => _long.ShouldEqual(DatabaseName.Physical(new string('t', 60) + "_long_names", DatabaseType.PostgreSql));
    [Fact] void should_keep_the_postgresql_schema_within_the_limit() => System.Text.Encoding.UTF8.GetByteCount(_long).ShouldBeLessThanOrEqual(63);
    [Fact] void should_keep_the_sql_server_schema_within_the_limit() => _sqlServerLong.Length.ShouldBeLessThanOrEqual(128);
    [Fact] void should_have_no_schema_for_sqlite() => _sqlite.ShouldBeNull();
}
