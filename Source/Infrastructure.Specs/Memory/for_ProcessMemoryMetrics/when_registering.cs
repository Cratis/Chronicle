// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Memory.for_ProcessMemoryMetrics;

public class when_registering : Specification
{
    Meter _meter;
    readonly List<string> _instruments = [];
    readonly Dictionary<string, List<(long Value, string? Generation)>> _longs = [];
    readonly Dictionary<string, List<double>> _doubles = [];

    void Establish()
    {
        _meter = new($"process-memory-{Guid.NewGuid():N}");
        GC.Collect();
    }

    void Because()
    {
        ProcessMemoryMetrics.Register(_meter);
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == _meter)
            {
                _instruments.Add(instrument.Name);
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (!_longs.TryGetValue(instrument.Name, out var list))
            {
                list = [];
                _longs[instrument.Name] = list;
            }

            list.Add((value, tags.ToArray().FirstOrDefault(tag => tag.Key == "generation").Value as string));
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (!_doubles.TryGetValue(instrument.Name, out var list))
            {
                list = [];
                _doubles[instrument.Name] = list;
            }

            list.Add(value);
        });
        listener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
        {
            if (!_longs.TryGetValue(instrument.Name, out var list))
            {
                list = [];
                _longs[instrument.Name] = list;
            }

            list.Add((value, null));
        });
        listener.Start();
        listener.RecordObservableInstruments();
    }

    void Destroy() => _meter.Dispose();

    [Theory]
    [InlineData("cratis.chronicle.memory.heap_size")]
    [InlineData("cratis.chronicle.memory.committed")]
    [InlineData("cratis.chronicle.memory.available")]
    [InlineData("cratis.chronicle.memory.load")]
    [InlineData("cratis.chronicle.memory.fragmentation")]
    [InlineData("cratis.chronicle.memory.pause_time_ratio")]
    [InlineData("cratis.chronicle.memory.working_set")]
    [InlineData("cratis.chronicle.memory.allocated")]
    [InlineData("cratis.chronicle.memory.gen2_collections")]
    void should_register_the_instrument(string name) => _instruments.ShouldContain(name);

    [Fact] void should_report_committed_memory() => _longs["cratis.chronicle.memory.committed"].Single().Value.ShouldBeGreaterThan(0L);
    [Fact] void should_report_allocated_bytes() => _longs["cratis.chronicle.memory.allocated"].Single().Value.ShouldBeGreaterThan(0L);
}
