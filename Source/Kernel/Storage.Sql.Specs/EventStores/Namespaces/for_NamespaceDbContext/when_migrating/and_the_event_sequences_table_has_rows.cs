// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceDbContext.when_migrating;

public class and_the_event_sequences_table_has_rows : given.a_migrated_namespace_database
{
    EventSequences.EventSequenceState _row;
    long _nullableColumns;

    async Task Establish()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("DROP TABLE EventSequences");
        var original = new EventSequences.Migrations.v15_0_0 { ActiveProvider = context.Database.ProviderName! };
        foreach (var command in context.GetService<IMigrationsSqlGenerator>().Generate(original.UpOperations))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await context.Database.ExecuteSqlRawAsync(context.GetService<IHistoryRepository>().GetDeleteScript("NS-EventSequences-v19_79_1"));
        await context.Database.ExecuteSqlRawAsync("INSERT INTO EventSequences (EventSequenceId, SequenceNumber, TailSequenceNumberPerEventType) VALUES ('event-log', 42, '{{}}')");
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        _row = await context.EventSequences.SingleAsync();
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('EventSequences') WHERE name IN ('ConstraintIndexWatermark', 'ConstraintIndexCatchUpBelow', 'ConstraintIndexCatchUpRefusing') AND [notnull] = 0";
        _nullableColumns = Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    [Fact] void should_keep_the_existing_sequence_number() => _row.SequenceNumber.ShouldEqual(42UL);
    [Fact] void should_leave_the_watermark_null() => _row.ConstraintIndexWatermark.ShouldBeNull();
    [Fact] void should_leave_the_boundary_null() => _row.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_leave_the_refusal_null() => _row.ConstraintIndexCatchUpRefusing.ShouldBeNull();
    [Fact] void should_add_three_nullable_columns() => _nullableColumns.ShouldEqual(3L);
}
