// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_another_kernel_adds_the_initialization_column : given.a_read_model_migrator
{
    const string TableName = "concurrently_upgraded";
    IReadOnlyList<ProjectedColumn> _columns;
    IReadOnlySet<string> _actualColumns;
    bool _competingMigrationExecuted;

    async Task Establish()
    {
        var columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"}}}
            """));
        await using var oldContext = CreateContext(TableName, columns.Where(column => column.Name != WellKnownProperties.ReadModelInstanceInitialized).ToArray());
        await _migrator.EnsureTableMigrated(TableName, oldContext);

        _columns = [.. columns, new ProjectedColumn("later_property", typeof(string), false, false, false, true)];
        var realMigrator = new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>());
        var competingMigrator = Substitute.For<ITableMigrator<ReadModelDbContext>>();
        competingMigrator.ExecuteMigrationOperations(Arg.Any<ReadModelDbContext>(), Arg.Any<MigrationBuilder>())
            .Returns(async call =>
            {
                var migration = call.ArgAt<MigrationBuilder>(1);
                if (!_competingMigrationExecuted)
                {
                    _competingMigrationExecuted = true;
                    var competingMigration = new MigrationBuilder(migration.ActiveProvider);
                    competingMigration.Operations.Add(migration.Operations[0]);
                    await using var competingContext = CreateContext(TableName, _columns);
                    await realMigrator.ExecuteMigrationOperations(competingContext, competingMigration);
                }

                await realMigrator.ExecuteMigrationOperations(call.ArgAt<ReadModelDbContext>(0), migration);
            });
        competingMigrator.ColumnExists(Arg.Any<ReadModelDbContext>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => realMigrator.ColumnExists(call.ArgAt<ReadModelDbContext>(0), call.ArgAt<string>(1), call.ArgAt<string>(2)));
        _migrator = new ReadModelMigrator(competingMigrator, Substitute.For<ILogger<ReadModelMigrator>>());
    }

    async Task Because()
    {
        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        await _migrator.EnsureTableMigrated(TableName, context);
        _actualColumns = await GetActualColumnNamesAsync(TableName);
    }

    [Fact] void should_exercise_a_competing_column_addition() => _competingMigrationExecuted.ShouldBeTrue();
    [Fact] void should_accept_the_column_added_by_the_other_kernel() => _actualColumns.ShouldContain(WellKnownProperties.ReadModelInstanceInitialized);
    [Fact] void should_still_add_the_remaining_columns() => _actualColumns.ShouldContain("later_property");
}
