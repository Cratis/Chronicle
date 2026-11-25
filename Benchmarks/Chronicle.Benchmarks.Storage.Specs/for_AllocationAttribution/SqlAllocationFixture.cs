// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Benchmarks.Storage.for_AllocationAttribution;

/// <summary>Identical SQLite datasets and driver instrumentation, with only the harness arm changed.</summary>
/// <param name="legacy">Whether to use the unchanged linked host and baseline substitutes.</param>
internal sealed class SqlAllocationFixture(bool legacy) : IDisposable
{
    /// <summary>The seeded event count; both arms use 256-byte payloads and the default 100-event cursor batch.</summary>
    public const int EventCount = 256;

    readonly SqliteConnection _connection = new($"Data Source=alloc_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
    readonly EventType[] _types = [BenchmarkEvents.Selected, BenchmarkEvents.Other];
    readonly CommandCounter _counter = new();
    readonly QueryCapture _capture = new();
    SqlBenchmarkOptions _options = null!;
    BenchmarkDatabase _database = null!;
    EventSequenceStorageHost? _legacyHost;
    IEventSequenceStorage _storage = null!;
    IInterceptor[] _interceptors = [];
    IDatabase _recordingDatabase = null!;

    /// <summary>Gets the number of events returned by the latest full drain.</summary>
    public int Drained { get; private set; }

    /// <summary>Gets the commands issued by the latest full drain.</summary>
    public long DrainCommands { get; private set; }

    /// <summary>Gets the cumulative sequence-number checksum from the latest full drain.</summary>
    public ulong Checksum { get; private set; }

    /// <summary>Initializes schema and data outside all samples.</summary>
    /// <exception cref="InvalidOperationException">Fixture seeding fails.</exception>
    public void Start()
    {
        _connection.Open();
        _interceptors = CountingInterceptors.For(_counter, _capture);
        _options = new(StorageProvider.Sqlite, _connection.ConnectionString, _interceptors);
        _database = new(_options);
        using (var context = new NamespaceDbContext(_options.Namespace)) context.Database.Migrate();
        _storage = legacy ? OpenLegacy() : OpenClean();
        var appended = AllocationProbe.Complete(_storage.AppendMany(Enumerable.Range(0, EventCount).Select(i => BenchmarkEvents.ToAppend((ulong)i, 256))));
        if (!appended.IsSuccess) throw new InvalidOperationException("Allocation fixture seeding failed.");
        _recordingDatabase = Substitute.For<IDatabase>();
        _recordingDatabase.Namespace(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>())
            .Returns(_ => NewCachedScope());
    }

    /// <summary>Measures the same GetState boundary used by activation.</summary>
    public void State() => GC.KeepAlive(AllocationProbe.Complete(_storage.GetState()));

    /// <summary>Measures the same overall-tail boundary used by activation.</summary>
    public void Tail() => GC.KeepAlive(AllocationProbe.Complete(_storage.GetTailSequenceNumber()));

    /// <summary>Measures per-type tails with a preconstructed filter.</summary>
    public void TypeTails() => GC.KeepAlive(AllocationProbe.Complete(_storage.GetTailSequenceNumbersForEventTypes(_types)));

    /// <summary>Drains and consumes every event, including every synchronously-completed MoveNext.</summary>
    /// <exception cref="InvalidOperationException">Payload conversion changes.</exception>
    public void Drain()
    {
        Drained = 0;
        Checksum = 0;
        using var cursor = AllocationProbe.Complete(_storage.GetFromSequenceNumber(0));
        while (AllocationProbe.Complete(cursor.MoveNext()))
        {
            foreach (var @event in cursor.Current)
            {
                Drained++;
                Checksum += @event.Context.SequenceNumber.Value;
                if (((IDictionary<string, object?>)@event.Content)["value"] is not string { Length: 256 }) throw new InvalidOperationException("Payload conversion changed.");
            }
        }
    }

    /// <summary>Validates driver command counts outside the sampled window.</summary>
    public void ValidateDrain()
    {
        _counter.Reset();
        Drain();
        DrainCommands = _counter.Snapshot().Total;
    }

    /// <summary>Builds the options that the baseline rebuilt at each namespace call site.</summary>
    public void BuildOptions() => GC.KeepAlive(SqlBenchmarkOptions.Build<NamespaceDbContext>(StorageProvider.Sqlite, _connection.ConnectionString, _interceptors));

    /// <summary>Reads the same immutable options reused by the clean adapter.</summary>
    public void CachedOptions() => GC.KeepAlive(_options.Namespace);

    /// <summary>Isolates dispatch/context/scope allocations with cached options on both arms.</summary>
    public void CachedScope()
    {
        using var scope = AllocationProbe.Complete((legacy ? _recordingDatabase : _database).Namespace("test-store", "test-namespace"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _legacyHost?.Dispose();
        _connection.Dispose();
    }

    IEventSequenceStorage OpenLegacy()
    {
        _legacyHost = new(
            builder => builder.UseSqlite(_connection.ConnectionString).EnableServiceProviderCaching(true),
            null,
            LegacyFixtures.Identities(),
            LegacyFixtures.EventTypes(),
            _interceptors);
        return _legacyHost.Open(() => new NamespaceDbContext(SqlBenchmarkOptions.Build<NamespaceDbContext>(StorageProvider.Sqlite, _connection.ConnectionString, _interceptors)));
    }

    IEventSequenceStorage OpenClean()
    {
        var table = $"events_{Guid.NewGuid():N}";
        using (var scope = _database.CreateEventSequenceScope(table)) AllocationProbe.Complete(scope.DbContext.EnsureTableExists());
        return new EventSequenceStorage(
            "test-store",
            "test-namespace",
            new EventSequenceId(table),
            _database,
            BenchmarkEvents.CreateIdentities(),
            NullLogger<EventSequenceStorage>.Instance,
            BenchmarkEvents.CreateEventTypes(),
            new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
    }

    DbContextScope<NamespaceDbContext> NewCachedScope()
    {
#pragma warning disable CA2000 // The scope owns and disposes the context.
        return new(new(_options.Namespace), () => { });
#pragma warning restore CA2000
    }
}
