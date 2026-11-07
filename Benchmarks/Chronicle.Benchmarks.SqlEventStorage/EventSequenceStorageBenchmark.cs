// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;
using CorrelationId = Cratis.Execution.CorrelationId;

namespace Cratis.Chronicle.Benchmarks.SqlEventStorage;

/// <summary>Measures SQL storage append and ordinary read paths without the kernel.</summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 15)]
public class EventSequenceStorageBenchmark : IAsyncDisposable
{
    static readonly EventType _eventType = new("benchmark-event", EventTypeGeneration.First);
    SqlBenchmarkDatabase _database = null!;
    EventSequenceStorageHost _host = null!;
    IEventSequenceStorage _storage = null!;
    ulong _next = 10000;
    int _read;

    /// <summary>Gets or sets the SQL provider.</summary>
    [Params(SqlStorageProvider.PostgreSql, SqlStorageProvider.SqlServer)]
    public SqlStorageProvider Provider { get; set; }

    /// <summary>Gets or sets the fraction of revised seeded rows.</summary>
    [Params(0, 0.01)]
    public double RevisedFraction { get; set; }

    /// <summary>Seeds ten thousand events, outside measurement.</summary>
    /// <returns>The setup task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _database = new();
        await _database.Start(Provider);
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        var types = Substitute.For<IEventTypesStorage>();
        _host = Provider == SqlStorageProvider.PostgreSql
            ? EventSequenceStorageHost.ForPostgreSql(_database.ConnectionString, identities, types, cacheServiceProvider: true)
            : EventSequenceStorageHost.ForSqlServer(_database.ConnectionString, identities, types, cacheServiceProvider: true);
        _storage = _host.Open();
        await using (var context = _host.CreateContext())
        {
            context.Events.AddRange(Enumerable.Range(0, 10000).Select(number => EventEntryConverter.ToEventEntry(
                (ulong)number, EventSourceType.Default, $"source-{number % 100}", EventStreamType.All, EventStreamId.Default, number % 2 == 0 ? _eventType : new("benchmark-other", EventTypeGeneration.First), CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = Content() }, new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "hash" })));
            await context.SaveChangesAsync();
        }
        for (var index = 0; index < (int)(10000 * RevisedFraction); index++)
        {
            await _storage.Revise((ulong)(index * 100), _eventType, CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow, Content(), "revised-hash");
        }
    }

    /// <summary>Disposes the isolated database.</summary>
    /// <returns>The cleanup task.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _host?.Dispose();
        if (_database is not null) await _database.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Appends one event.</summary>
    /// <returns>The append task.</returns>
    /// <exception cref="InvalidOperationException">The append failed.</exception>
    [Benchmark]
    public async Task AppendSingle()
    {
        var number = _next++;
        var result = await _storage.Append(number, EventSourceType.Default, "benchmark-source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = Content() }, new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "hash" });
        if (!result.IsSuccess) throw new InvalidOperationException("Append failed");
    }

    /// <summary>Appends a batch of one hundred events.</summary>
    /// <returns>The append task.</returns>
    /// <exception cref="InvalidOperationException">The batch append failed.</exception>
    [Benchmark]
    public async Task AppendMany100()
    {
        var first = _next;
        _next += 100;
        var result = await _storage.AppendMany(Enumerable.Range(0, 100).Select(offset => new EventToAppendToStorage(first + (ulong)offset, EventSourceType.Default, "benchmark-source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, Content(), "hash")));
        if (!result.IsSuccess) throw new InvalidOperationException("Batch append failed");
    }

    /// <summary>Reads a single event.</summary>
    /// <returns>The event.</returns>
    [Benchmark]
    public Task<AppendedEvent> GetEventAt() => _storage.GetEventAt((ulong)(Interlocked.Increment(ref _read) & 255));

    /// <summary>Reads a bounded thousand-event range.</summary>
    /// <returns>The event count.</returns>
    [Benchmark]
    public async Task<int> GetRange1000() => await Drain(await _storage.GetRange(0, 999));

    /// <summary>Reads catch-up events narrowed by event type.</summary>
    /// <returns>The event count.</returns>
    [Benchmark]
    public async Task<int> CatchUpByEventType() => await Drain(await _storage.GetRange(0, 9999, eventTypes: [_eventType]));

    /// <summary>Reads a cursor from a deep sequence number.</summary>
    /// <returns>The event count.</returns>
    [Benchmark]
    public async Task<int> CursorFromSequenceNumber() => await Drain(await _storage.GetFromSequenceNumber(5000));

    static ExpandoObject Content()
    {
        dynamic content = new ExpandoObject();
        content.value = "benchmark";
        return content;
    }

    static async Task<int> Drain(IEventCursor cursor)
    {
        using (cursor)
        {
            var count = 0;
            while (await cursor.MoveNext()) count += cursor.Current.Count();
            return count;
        }
    }
}
