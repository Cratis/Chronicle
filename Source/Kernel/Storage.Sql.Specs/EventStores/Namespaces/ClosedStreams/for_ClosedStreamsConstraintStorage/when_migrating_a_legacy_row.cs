// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

public class when_migrating_a_legacy_row : given.an_unmigrated_closed_streams_database
{
    IEnumerable<ClosedStream> _rows;

    async Task Establish()
    {
        await using var context = CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();
        var history = context.GetService<IHistoryRepository>();
        var sqlGenerator = context.GetService<IMigrationsSqlGenerator>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());

        // These migration IDs are not EF timestamp IDs. Apply the prior schema and history directly,
        // matching the other namespace upgrade specifications, then use Migrate for the real upgrade.
        foreach (var (id, type) in assembly.Migrations.Where(entry => entry.Key != "NS-ClosedStreams-v19_38_0"))
        {
            var migration = assembly.CreateMigration(type, context.Database.ProviderName);
            foreach (var command in sqlGenerator.Generate(migration.UpOperations))
            {
                await context.Database.ExecuteSqlRawAsync(command.CommandText);
            }
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.0")));
        }
        await context.Database.ExecuteSqlRawAsync("INSERT INTO ClosedStreams (EventSequenceId, StreamType, StreamId) VALUES ('event-log', 'transactions', 'month')");
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        _rows = await _storage.GetAll();
    }

    [Fact] void should_read_one_legacy_scope() => _rows.Count().ShouldEqual(1);
    [Fact] void should_preserve_stream_dimensions() => _rows.Single().Scope.ShouldEqual(new ClosedStreamScope(EventStreamType: "transactions", EventStreamId: "month"));
    [Fact] void should_read_manual_owner() => _rows.Single().Owner.ShouldEqual(ClosedStreamOwner.Manual);
    [Fact] void should_read_unavailable_sequence_number() => _rows.Single().SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_read_no_timestamp() => _rows.Single().ClosedAt.ShouldBeNull();
}
