// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers.given;

/// <summary>
/// Builds the real event sequence and in-flight storage over a provider whose commands are captured and not run.
/// </summary>
/// <param name="useSqlServer">Whether to target SQL Server, otherwise SQLite.</param>
public abstract class a_storage_that_captures_statements(bool useSqlServer) : Specification
{
    protected const string SourceSeek = "[EventSourceId] = @";
    protected const string SourceTypeSeek = "[EventSourceType] = @";
    protected const string StreamTypeSeek = "[EventStreamType] = @";
    protected const string StreamIdSeek = "[EventStreamId] = @";
    protected const string PartitionSeek = "[Partition] = @";

    protected readonly CapturedStatements _captured = new();
    protected EventSequenceStorage _events;
    protected InFlightEventsStorage _inFlight;

    void Establish()
    {
        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(
                new EventSequenceDbContext(Options<EventSequenceDbContext>(), "events", Substitute.For<IEventSequenceMigrator>()),
                () => { })));
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new NamespaceDbContext(Options<NamespaceDbContext>()), () => { })));
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _events = new(
            "capture-store",
            "capture-namespace",
            new EventSequenceId("events"),
            database,
            identities,
            Substitute.For<ILogger<EventSequenceStorage>>(),
            Substitute.For<Chronicle.Storage.EventTypes.IEventTypesStorage>(),
            new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
        _inFlight = new("capture-store", "capture-namespace", database);
    }

    protected async Task ReadBySource(EventSourceId eventSourceId)
    {
        using var cursor = await _events.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId);
        await cursor.MoveNext();
    }

    protected async Task ReadBySourceType(EventSourceType eventSourceType)
    {
        using var cursor = await _events.GetFromSequenceNumber(EventSequenceNumber.First, default, eventSourceType);
        await cursor.MoveNext();
    }

    protected async Task ReadByStream(EventStreamType eventStreamType, EventStreamId eventStreamId)
    {
        using var cursor = await _events.GetFromSequenceNumber(EventSequenceNumber.First, default, default, eventStreamType, eventStreamId);
        await cursor.MoveNext();
    }

    protected Task RemoveFromInFlight(Key partition) => _inFlight.Remove(new ObserverId("observer"), partition, EventSequenceNumber.First);

    protected string Only() => _captured.All.Single();

    DbContextOptions<TContext> Options<TContext>()
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>().AddConceptAsSupport().WithoutServiceProviderCaching().AddInterceptors(_captured.CreateInterceptors());
        return useSqlServer
            ? builder.UseSqlServer("Server=.;Database=x").Options
            : builder.UseSqlite("Data Source=:memory:").Options;
    }
}
