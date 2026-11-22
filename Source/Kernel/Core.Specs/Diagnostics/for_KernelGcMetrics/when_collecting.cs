// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Diagnostics.for_KernelGcMetrics;

public class when_collecting : Specification
{
    Meter _meter;
    KernelGcMetrics _metrics;
    readonly List<Instrument> _instruments = [];
    readonly List<(string Name, long Value, string? Generation)> _longs = [];
    readonly List<(string Name, double Value)> _doubles = [];

    void Establish()
    {
        _meter = new($"gc-metrics-{Guid.NewGuid():N}");
        var meter = Substitute.For<IMeter<KernelGcMetrics>>();
        meter.ActualMeter.Returns(_meter);
        GC.Collect();
        _metrics = new(meter);
    }

    void Because()
    {
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == _meter)
            {
                _instruments.Add(instrument);
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            _longs.Add((instrument.Name, value, tags.ToArray().FirstOrDefault(tag => tag.Key == "generation").Value as string)));
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => _doubles.Add((instrument.Name, value)));
        listener.Start();
        listener.RecordObservableInstruments();
    }

    void Destroy() => _meter.Dispose();

    [Fact] void should_register_the_seven_instruments() => _instruments.Count.ShouldEqual(7);
    [Fact] void should_report_every_generation_size() => _longs.Where(_ => _.Name == "cratis.chronicle.gc.heap.generation.size").Select(_ => _.Generation).ShouldContainOnly("gen0", "gen1", "gen2", "loh", "poh");
    [Fact] void should_report_every_generation_fragmentation() => _longs.Where(_ => _.Name == "cratis.chronicle.gc.heap.generation.fragmentation").Select(_ => _.Generation).ShouldContainOnly("gen0", "gen1", "gen2", "loh", "poh");
    [Fact] void should_report_committed_memory() => _longs.Single(_ => _.Name == "cratis.chronicle.gc.heap.committed").Value.ShouldBeGreaterThan(0L);
    [Fact] void should_report_the_heap_size() => _longs.Single(_ => _.Name == "cratis.chronicle.gc.heap.size").Value.ShouldBeGreaterThan(0L);
    [Fact] void should_report_allocated_bytes() => _longs.Single(_ => _.Name == "cratis.chronicle.gc.allocated").Value.ShouldBeGreaterThan(0L);
    [Fact] void should_report_collections_for_three_generations() => _longs.Where(_ => _.Name == "cratis.chronicle.gc.collections").Select(_ => _.Generation).ShouldContainOnly("gen0", "gen1", "gen2");
    [Fact] void should_report_the_pause_total() => _doubles.Single(_ => _.Name == "cratis.chronicle.gc.pause.duration").Value.ShouldBeGreaterThanOrEqual(0.0);
}
