// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Benchmarks.Serialization;

/// <summary>
/// Measures the Orleans appended-event wire path without a silo or Docker.
/// </summary>
[MemoryDiagnoser]
[InProcess]
[WarmupCount(1)]
[IterationCount(3)]
[MinIterationTime(100)]
public class AppendedEventCodecBenchmarks
{
    readonly ArrayBufferWriter<byte> _buffer = new();
    ServiceProvider _provider = null!;
    Serializer<List<AppendedEvent>> _serializer = null!;
    List<AppendedEvent> _events = null!;
    byte[] _serialized = [];

    /// <summary>
    /// Gets or sets the producer-normalized payload shape.
    /// </summary>
    [Params("Small", "Medium", "Large")]
    public string Shape { get; set; } = "Small";

    /// <summary>
    /// Gets or sets the number of events in a message.
    /// </summary>
    [Params(1, 100)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the wire format; the baseline uses the released JSON encoding.
    /// </summary>
    public string Format { get; set; } = "Json";

    /// <summary>
    /// Initializes serialization and warms both schema caches outside measurement.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _provider = AppendedEventPayloads.CreateProvider(Shape, out var appendedEvent);
        _serializer = _provider.GetRequiredService<Serializer<List<AppendedEvent>>>();
        _events = Enumerable.Range(0, BatchSize).Select(index => appendedEvent with
        {
            Context = appendedEvent.Context with { SequenceNumber = (ulong)index }
        }).ToList();
        _serialized = _serializer.SerializeToArray(_events);
        _ = _serializer.Deserialize(_serialized);
    }

    /// <summary>
    /// Releases the serializer services.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    /// <summary>
    /// Serializes a batch into a reused output buffer.
    /// </summary>
    /// <returns>The number of bytes written.</returns>
    [Benchmark(Baseline = true)]
    public int Serialize()
    {
        _buffer.Clear();
        _serializer.Serialize(_events, _buffer);
        return _buffer.WrittenCount;
    }

    /// <summary>
    /// Deserializes a previously encoded batch.
    /// </summary>
    /// <returns>The decoded events.</returns>
    [Benchmark]
    public IReadOnlyList<AppendedEvent> Deserialize() => _serializer.Deserialize(_serialized)!;

    /// <summary>
    /// Serializes and deserializes one batch across the wire boundary.
    /// </summary>
    /// <returns>The decoded events.</returns>
    [Benchmark]
    public IReadOnlyList<AppendedEvent> RoundTrip()
    {
        _buffer.Clear();
        _serializer.Serialize(_events, _buffer);
        return _serializer.Deserialize(_buffer.WrittenMemory)!;
    }
}
