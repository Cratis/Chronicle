// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_migrating_a_legacy_postgresql_row(PostgreSqlFixture fixture) : Specification
{
    string _connectionString;
    ClosedStreamsConstraintStorage _storage;
    IEnumerable<ClosedStream> _rows;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => new DbContextScope<NamespaceDbContext>(CreateContext(), () => { }));
        _storage = new("test-store", "test-namespace", EventSequenceId.Log, database);

        await using var context = CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();
        var history = context.GetService<IHistoryRepository>();
        var sqlGenerator = context.GetService<IMigrationsSqlGenerator>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
        foreach (var (id, type) in assembly.Migrations.Where(entry => entry.Key != "NS-ClosedStreams-v19_38_0"))
        {
            var migration = assembly.CreateMigration(type, context.Database.ProviderName);
            foreach (var command in sqlGenerator.Generate(migration.UpOperations))
            {
                await context.Database.ExecuteSqlRawAsync(command.CommandText);
            }
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.0")));
        }
        await context.Database.ExecuteSqlRawAsync("INSERT INTO \"ClosedStreams\" (\"EventSequenceId\", \"StreamType\", \"StreamId\") VALUES ('event-log', 'transactions', 'month')");
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        _rows = await _storage.GetAll();
    }

    Task Destroy() => fixture.DropDatabase(_connectionString);

    [Fact] void should_read_one_legacy_scope() => _rows.Count().ShouldEqual(1);
    [Fact] void should_preserve_stream_dimensions() => _rows.Single().Scope.ShouldEqual(new ClosedStreamScope(EventStreamType: "transactions", EventStreamId: "month"));
    [Fact] void should_read_manual_owner() => _rows.Single().Owner.ShouldEqual(ClosedStreamOwner.Manual);
    [Fact] void should_read_unavailable_sequence_number() => _rows.Single().SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);

    NamespaceDbContext CreateContext() => new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseNpgsql(_connectionString).AddConceptAsSupport().Options);
}
