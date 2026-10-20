// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

public class when_naming_indexes : Specification
{
    string _postgres;
    string _sqlServer;
    string _table;
    string _column;

    void Establish()
    {
        _table = string.Concat(Enumerable.Repeat("é表😀", 25));
        _column = string.Concat(Enumerable.Repeat("column\"'[]", 20));
    }

    void Because()
    {
        _postgres = ReadModelIndexNames.For(DatabaseType.PostgreSql, _table, _column);
        _sqlServer = ReadModelIndexNames.For(DatabaseType.SqlServer, _table, _column);
    }

    [Fact] void should_bound_postgresql_bytes() => (Encoding.UTF8.GetByteCount(_postgres) <= 63).ShouldBeTrue();
    [Fact] void should_bound_sql_server_characters() => (_sqlServer.Length <= 128).ShouldBeTrue();
    [Fact] void should_be_deterministic() => _postgres.ShouldEqual(ReadModelIndexNames.For(DatabaseType.PostgreSql, _table, _column));
    [Fact] void should_distinguish_columns_after_truncation() => _postgres.ShouldNotEqual(ReadModelIndexNames.For(DatabaseType.PostgreSql, _table, _column + "other"));
    [Fact] void should_distinguish_shadow_tables() => _postgres.ShouldNotEqual(ReadModelIndexNames.For(DatabaseType.PostgreSql, "replay-" + _table, _column));
    [Fact] void should_not_alias_table_column_boundaries() => ReadModelIndexNames.For(DatabaseType.Sqlite, "a_b", "c").ShouldNotEqual(ReadModelIndexNames.For(DatabaseType.Sqlite, "a", "b_c"));
}
