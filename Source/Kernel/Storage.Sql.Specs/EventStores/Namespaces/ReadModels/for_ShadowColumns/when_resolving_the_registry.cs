// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumns;

public class when_resolving_the_registry : Specification
{
    IReadOnlyList<AddColumnOperation> _sqlite;
    IReadOnlyList<AddColumnOperation> _postgres;
    IReadOnlyList<AddColumnOperation> _sqlServer;
    string[] _projected;

    void Because()
    {
        var columns = ProjectedColumns.ForSchema(JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"number","format":"decimal"},"amount":{"type":"number","format":"decimal"},"other":{"type":"number"},"nested":{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}}}"""));
        _projected = columns.Select(column => column.Name).ToArray();
        using var sqlite = new ReadModelDbContext(new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite("Data Source=:memory:").Options, "decimals", columns, Substitute.For<IReadModelMigrator>());
        using var postgres = new ReadModelDbContext(new DbContextOptionsBuilder<ReadModelDbContext>().UseNpgsql("Host=localhost;Database=not_opened").Options, "decimals", columns, Substitute.For<IReadModelMigrator>());
        using var sqlServer = new ReadModelDbContext(new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlServer("Server=localhost;Database=not_opened").Options, "decimals", columns, Substitute.For<IReadModelMigrator>());
        _sqlite = ShadowColumns.For(sqlite);
        _postgres = ShadowColumns.For(postgres);
        _sqlServer = ShadowColumns.For(sqlServer);
    }

    [Fact] void should_include_decimal_keys_and_nullable_properties_only() => _sqlite.Select(column => column.Name).ShouldEqual(["__order_id", "__order_amount"]);
    [Fact] void should_keep_the_columns_nullable_without_a_default() => _sqlite.All(column => column.IsNullable && column.DefaultValue is null && column.DefaultValueSql is null).ShouldBeTrue();
    [Fact] void should_generate_virtual_text_columns() => _sqlite.All(column => column.ComputedColumnSql is not null && column.IsStored == false && column.ColumnType == "TEXT").ShouldBeTrue();
    [Fact] void should_never_add_shadows_to_projected_columns() => _projected.Any(name => name.StartsWith("__order_", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_apply_decimal_ordering_to_postgresql() => _postgres.ShouldBeEmpty();
    [Fact] void should_not_apply_decimal_ordering_to_sql_server() => _sqlServer.ShouldBeEmpty();
}
