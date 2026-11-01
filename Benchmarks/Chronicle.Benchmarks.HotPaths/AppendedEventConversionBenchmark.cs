// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using System.Collections.Immutable;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Contracts.Sequences;
using ClientAppendedEvent = Client::Cratis.Chronicle.Events.AppendedEvent;
using ClientEventStoreName = Client::Cratis.Chronicle.EventStoreName;
using ClientEventStoreNamespaceName = Client::Cratis.Chronicle.EventStoreNamespaceName;
using ClientEventTypes = Client::Cratis.Chronicle.Events.IEventTypes;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Converts a batch of wire events to client events when none of them has erased properties, the common read path.
/// </summary>
[MemoryDiagnoser]
public class AppendedEventConversionBenchmark
{
    const int BatchSize = 100;
    readonly ClientEventStoreName _eventStore = new("store");
    readonly ClientEventStoreNamespaceName _namespace = new("ns");
    readonly JsonSerializerOptions _jsonSerializerOptions = new();
    readonly ClientEventTypes _eventTypes = new BenchmarkEventTypes();
    AppendedEventResponse[] _events = [];
    Func<IEnumerable<AppendedEventResponse>, ClientEventStoreName, ClientEventStoreNamespaceName, ClientEventTypes, JsonSerializerOptions, IImmutableList<ClientAppendedEvent>> _convert = null!;

    /// <summary>
    /// Builds the batch and binds the internal client converter, which is not part of the public surface.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _events = Enumerable.Range(0, BatchSize).Select(i => new AppendedEventResponse
        {
            Id = i.ToString(),
            Content = "{\"name\":\"value\",\"number\":42}",
            Context = new EventContext
            {
                EventType = new EventType { Id = "bench-event", Generation = 1 },
                EventSourceId = Guid.NewGuid().ToString(),
                SequenceNumber = (ulong)i,
                Occurred = DateTimeOffset.UtcNow,
                CorrelationId = Guid.NewGuid()
            }
        }).ToArray();

        var converters = typeof(ClientEventTypes).Assembly.GetType("Cratis.Chronicle.Events.AppendedEventConverters")!;
        var method = converters.GetMethod(
            "ToClient",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public,
            [typeof(IEnumerable<AppendedEventResponse>), typeof(ClientEventStoreName), typeof(ClientEventStoreNamespaceName), typeof(ClientEventTypes), typeof(JsonSerializerOptions)])!;
        _convert = method.CreateDelegate<Func<IEnumerable<AppendedEventResponse>, ClientEventStoreName, ClientEventStoreNamespaceName, ClientEventTypes, JsonSerializerOptions, IImmutableList<ClientAppendedEvent>>>();
    }

    /// <summary>
    /// Converts the batch.
    /// </summary>
    /// <returns>The converted events.</returns>
    [Benchmark]
    public IImmutableList<ClientAppendedEvent> ConvertBatch() => _convert(_events, _eventStore, _namespace, _eventTypes, _jsonSerializerOptions);
}
