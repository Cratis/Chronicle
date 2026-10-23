// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ReadModelIndexPlan;

public class when_lowering_ordered_paths : Specification
{
    JsonSchema _schema;
    IReadOnlyList<ProjectedColumn> _columns;
    ISqlGenerationHelper _sqlite;
    ISqlGenerationHelper _postgres;
    ISqlGenerationHelper _sqlServer;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"details":{"type":"object","properties":{"total":{"type":"number"},"count":{"type":"integer"},"active":{"type":"boolean"},"quote'key":{"type":"string"}}},"lines":{"type":"array","items":{"type":"object","properties":{"productId":{"type":"string"}}}}}}""");
        _columns = ProjectedColumns.ForSchema(_schema);
        await using var sqlite = new DbContext(new DbContextOptionsBuilder().UseSqlite("Data Source=:memory:").Options);
        await using var postgres = new DbContext(new DbContextOptionsBuilder().UseNpgsql("Host=localhost;Database=indexes").Options);
        await using var sqlServer = new DbContext(new DbContextOptionsBuilder().UseSqlServer("Server=localhost;Database=indexes").Options);
        _sqlite = sqlite.GetService<ISqlGenerationHelper>();
        _postgres = postgres.GetService<ISqlGenerationHelper>();
        _sqlServer = sqlServer.GetService<ISqlGenerationHelper>();
    }

    [Fact] void should_cast_postgresql_numbers() => Plan(new("details.total"), DatabaseType.PostgreSql).Keys(DatabaseType.PostgreSql, _postgres).ShouldEqual($"(({_postgres.DelimitIdentifier("details")} #>> ARRAY['total']::text[])::numeric) ASC");
    [Fact] void should_cast_postgresql_booleans() => Plan(new("details.active"), DatabaseType.PostgreSql).Keys(DatabaseType.PostgreSql, _postgres).ShouldContain("::boolean");
    [Fact] void should_extract_sqlite_scalars() => Plan(new("details.total"), DatabaseType.Sqlite).Keys(DatabaseType.Sqlite, _sqlite).ShouldEqual("json_extract(\"details\", '$.\"total\"') ASC");
    [Fact] void should_escape_sqlite_path_literals() => Plan(new("details.quote'key"), DatabaseType.Sqlite).Keys(DatabaseType.Sqlite, _sqlite).ShouldContain("quote''key");
    [Fact] void should_cast_sql_server_strings_to_a_bounded_key() => ReadModelIndexPlan.SqlServerExpression(Plan(new("details.quote'key"), DatabaseType.SqlServer).Targets[0], _sqlServer).ShouldContain("AS nvarchar(450)");
    [Fact] void should_cast_sql_server_numbers_to_text() => ReadModelIndexPlan.SqlServerExpression(Plan(new("details.total"), DatabaseType.SqlServer).Targets[0], _sqlServer).ShouldEqual("CAST(JSON_VALUE([details], N'$.\"total\"') AS nvarchar(450))");
    [Fact] void should_cast_sql_server_integers_to_text() => ReadModelIndexPlan.SqlServerExpression(Plan(new("details.count"), DatabaseType.SqlServer).Targets[0], _sqlServer).ShouldContain("AS nvarchar(450)");
    [Fact] void should_cast_sql_server_booleans_to_text() => ReadModelIndexPlan.SqlServerExpression(Plan(new("details.active"), DatabaseType.SqlServer).Targets[0], _sqlServer).ShouldContain("AS nvarchar(450)");
    [Fact] void should_use_gin_on_the_whole_json_column() => Plan(new("lines.productId"), DatabaseType.PostgreSql).Keys(DatabaseType.PostgreSql, _postgres).ShouldEqual($"{_postgres.DelimitIdentifier("lines")} jsonb_path_ops");
    [Fact] void should_collapse_array_paths_sharing_a_column() => Plan(new("lines"), DatabaseType.PostgreSql).Signature.ShouldEqual(Plan(new("lines.productId"), DatabaseType.PostgreSql).Signature);
    [Fact] void should_reject_sql_server_array_paths() => ReadModelIndexPlan.Resolve(new("lines.productId"), _schema, _columns, DatabaseType.SqlServer).ShouldBeNull();
    [Fact] void should_reject_sqlite_array_paths() => ReadModelIndexPlan.Resolve(new("lines.productId"), _schema, _columns, DatabaseType.Sqlite).ShouldBeNull();
    [Fact] void should_not_create_a_partial_composite_for_an_array() => ReadModelIndexPlan.Resolve(new("name") { Properties = [new("name"), new("lines.productId")] }, _schema, _columns, DatabaseType.PostgreSql).ShouldBeNull();
    [Fact] void should_reject_an_unknown_path() => ReadModelIndexPlan.Resolve(new("details.missing"), _schema, _columns, DatabaseType.Sqlite).ShouldBeNull();
    [Fact] void should_reject_a_nested_path_on_a_non_json_column() => ReadModelIndexPlan.Resolve(new("details.total"), _schema, _columns.Select(column => column with { IsJson = false }).ToArray(), DatabaseType.Sqlite).ShouldBeNull();
    [Fact] void should_reject_an_object_leaf() => ReadModelIndexPlan.Resolve(new("details"), _schema, _columns, DatabaseType.PostgreSql).ShouldBeNull();
    [Fact] void should_preserve_the_legacy_name() => Plan(new("name"), DatabaseType.Sqlite).Name(DatabaseType.Sqlite, "models").ShouldEqual(ReadModelIndexNames.For(DatabaseType.Sqlite, "models", "name"));
    [Fact] void should_keep_composite_order_and_direction() => Plan(new("name") { Properties = [new("name"), new("details.total", true)] }, DatabaseType.Sqlite).Keys(DatabaseType.Sqlite, _sqlite).ShouldEqual("\"name\" ASC, json_extract(\"details\", '$.\"total\"') DESC");
    [Fact]
    void should_rename_a_full_signature_without_losing_it()
    {
        var plan = Plan(new("details.total") { Properties = [new("details.total", true)] }, DatabaseType.PostgreSql);
        ReadModelIndexNames.AfterRename(DatabaseType.PostgreSql, "primary", plan.Name(DatabaseType.PostgreSql, "replay"), string.Empty).ShouldEqual(plan.Name(DatabaseType.PostgreSql, "primary"));
    }

    ReadModelIndexPlan Plan(IndexDefinition definition, DatabaseType type) => ReadModelIndexPlan.Resolve(definition, _schema, _columns, type)!;
}
