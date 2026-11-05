// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using K = KernelConcepts::Cratis.Chronicle.Concepts;
using Logging = Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.EventRevisions;

internal sealed class NewNodeStorage(string connectionString, string table, bool postgres)
{
    readonly EventSequenceMigrator _migrator = new(
        new TableMigrator<EventSequenceDbContext>(Logging.NullLogger<TableMigrator<EventSequenceDbContext>>.Instance),
        Logging.NullLogger<EventSequenceMigrator>.Instance);

    internal IEventSequenceStorage Open()
    {
        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable(Arg.Any<K.EventStoreName>(), Arg.Any<K.EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(Context(), static () => { })));
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<K.Identities.IdentityId>>()).Returns(K.Identities.Identity.System);
        return new EventSequenceStorage(
            "released-revisions",
            "Default",
            new(table),
            database,
            identities,
            Logging.NullLogger<EventSequenceStorage>.Instance,
            Substitute.For<IEventTypesStorage>(),
            new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
    }

    internal async Task EnsureTableExists()
    {
        _migrator.ClearMigrationCache(connectionString);
        await using var context = Context();
        await context.EnsureTableExists();
    }

    internal async Task<bool> TableExists()
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @table";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table";
        parameter.Value = table;
        command.Parameters.Add(parameter);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    internal async Task<EventEntry> Row(ulong sequenceNumber)
    {
        await using var context = Context();
        return await context.Events.AsNoTracking().SingleAsync(row => row.SequenceNumber == sequenceNumber);
    }

    internal async Task<EventEntry> WaitForRow(ulong sequenceNumber, Func<EventEntry, bool> applied, string expectation)
    {
        // Released kernels acknowledge Revise and Redact once the request is on the system sequence;
        // a reactor applies it to the row later, so wait for the committed row instead of the acknowledgement.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        do
        {
            var row = await Row(sequenceNumber);
            if (applied(row))
            {
                return row;
            }
        }
        while (await NextTick(timer, deadline.Token));

        throw new TimeoutException($"Released kernel did not commit '{expectation}' at {sequenceNumber}: {Describe(await Row(sequenceNumber))}");
    }

    internal async Task Revise(ulong sequenceNumber, Guid id)
    {
        var row = await Row(sequenceNumber);
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)[nameof(ClusterEventAppended.Id)] = id.ToString();
        await Open().Revise(new(sequenceNumber), new(row.Type, new(1)), new(Guid.NewGuid()), [], [], DateTimeOffset.UtcNow, content, new($"new-{id}"));
    }

    internal async Task<Guid> Read(ulong sequenceNumber)
    {
        var read = await Open().GetEventAt(new(sequenceNumber));
        return Guid.Parse(((IDictionary<string, object?>)read.Content)[nameof(ClusterEventAppended.Id)]!.ToString()!);
    }

    static async Task<bool> NextTick(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    static string Describe(EventEntry row) =>
        $"Type={row.Type}, RevisedGeneration={row.RevisedGeneration}, Content={row.Content}, ContentHashes={row.ContentHashes}, OriginalContent={row.OriginalContent ?? "<null>"}, Revisions={row.Revisions ?? "<null>"}";

    EventSequenceDbContext Context()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>();
        if (postgres) builder.UseNpgsql(connectionString);
        else builder.UseSqlServer(connectionString);
        builder.AddConceptAsSupport().EnableServiceProviderCaching(false);
        return new(builder.Options, table, _migrator);
    }
}
