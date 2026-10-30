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

public class when_lowering_sqlite_decimal_composites : Specification
{
    JsonSchema _schema;
    IReadOnlyList<ProjectedColumn> _columns;
    ISqlGenerationHelper _sqlite;
    ISqlGenerationHelper _postgres;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"amount":{"type":"number","format":"decimal"}}}""");
        _columns = ProjectedColumns.ForSchema(_schema);
        await using var sqlite = new DbContext(new DbContextOptionsBuilder().UseSqlite("Data Source=:memory:").Options);
        await using var postgres = new DbContext(new DbContextOptionsBuilder().UseNpgsql("Host=localhost;Database=indexes").Options);
        _sqlite = sqlite.GetService<ISqlGenerationHelper>();
        _postgres = postgres.GetService<ISqlGenerationHelper>();
    }

    [Fact] void should_key_sqlite_composites_by_the_order_column() => Composite(DatabaseType.Sqlite).Keys(DatabaseType.Sqlite, _sqlite).ShouldEqual("\"name\" ASC, \"__order_amount\" DESC");
    [Fact] void should_rename_the_sqlite_composite() => Composite(DatabaseType.Sqlite).Name(DatabaseType.Sqlite, "models").ShouldNotEqual(Composite(DatabaseType.Sqlite).PreviousName(DatabaseType.Sqlite, "models"));
    [Fact] void should_name_the_previous_sqlite_composite_by_its_original_signature() => Composite(DatabaseType.Sqlite).PreviousName(DatabaseType.Sqlite, "models").ShouldEqual(ReadModelIndexNames.ForSignature(DatabaseType.Sqlite, "models", Composite(DatabaseType.Sqlite).Signature));
    [Fact] void should_keep_postgresql_names() => Composite(DatabaseType.PostgreSql).Name(DatabaseType.PostgreSql, "models").ShouldEqual(Composite(DatabaseType.PostgreSql).PreviousName(DatabaseType.PostgreSql, "models"));
    [Fact] void should_keep_sql_server_names() => Composite(DatabaseType.SqlServer).Name(DatabaseType.SqlServer, "models").ShouldEqual(Composite(DatabaseType.SqlServer).PreviousName(DatabaseType.SqlServer, "models"));
    [Fact] void should_keep_postgresql_column_keys() => Composite(DatabaseType.PostgreSql).Keys(DatabaseType.PostgreSql, _postgres).ShouldEqual($"{_postgres.DelimitIdentifier("name")} ASC, {_postgres.DelimitIdentifier("amount")} DESC");
    [Fact] void should_not_use_the_order_key_for_postgresql() => Composite(DatabaseType.PostgreSql).UsesDecimalOrderKey(DatabaseType.PostgreSql).ShouldBeFalse();
    [Fact] void should_not_rename_composites_without_decimals() => Plan(new("name") { Properties = [new("name"), new("id")] }, DatabaseType.Sqlite).Name(DatabaseType.Sqlite, "models").ShouldEqual(Plan(new("name") { Properties = [new("name"), new("id")] }, DatabaseType.Sqlite).PreviousName(DatabaseType.Sqlite, "models"));

    ReadModelIndexPlan Composite(DatabaseType type) => Plan(new("name") { Properties = [new("name"), new("amount", true)] }, type);
    ReadModelIndexPlan Plan(IndexDefinition definition, DatabaseType type) => ReadModelIndexPlan.Resolve(definition, _schema, _columns, type)!;
}
