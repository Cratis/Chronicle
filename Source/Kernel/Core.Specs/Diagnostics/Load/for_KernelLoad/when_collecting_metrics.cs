// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_collecting_metrics : Specification
{
    readonly List<(string Instrument, double Value, string? GrainType, string? EventStore)> _measurements = [];
    Meter _meter;
    KernelLoad _load;

    void Establish()
    {
        _meter = new($"kernel-load-{Guid.NewGuid():N}");
        var time = new ManualTimeProvider();
        _load = new KernelLoad(Options.Create(new ChronicleOptions()), NullLogger<KernelLoad>.Instance, _meter, time);
        _load.BeginPending("IProjectionsManager", "the-store");
        _load.BeginRunning("IObserver", "the-store");
        _load.BeginRunning("IJobsManager", string.Empty);
        _load.StreamOpened();
        time.Advance(TimeSpan.FromSeconds(3));
    }

    void Because()
    {
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == _meter)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => Record(instrument, value, tags));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        listener.Start();
        listener.RecordObservableInstruments();
    }

    void Destroy() => _meter.Dispose();

    [Fact] void should_report_the_running_calls_per_grain_and_event_store() => Value(WellKnownMetricNames.GrainCallsRunning, "IObserver").ShouldEqual(1);
    [Fact] void should_report_the_pending_calls_per_grain_and_event_store() => Value(WellKnownMetricNames.GrainCallsPending, "IProjectionsManager").ShouldEqual(1);
    [Fact] void should_report_the_oldest_pending_age_in_seconds() => Value(WellKnownMetricNames.GrainCallsOldestPendingAge, "IProjectionsManager").ShouldEqual(3);
    [Fact] void should_tag_the_event_store_when_the_grain_has_one() => _measurements.Single(_ => _.Instrument == WellKnownMetricNames.GrainCallsRunning && _.GrainType == "IObserver").EventStore.ShouldEqual("the-store");
    [Fact] void should_leave_the_event_store_out_when_the_grain_has_none() => _measurements.Single(_ => _.Instrument == WellKnownMetricNames.GrainCallsRunning && _.GrainType == "IJobsManager").EventStore.ShouldBeNull();
    [Fact] void should_report_the_load_status() => _measurements.Single(_ => _.Instrument == WellKnownMetricNames.KernelLoadStatus).Value.ShouldEqual(0);
    [Fact] void should_report_the_open_streams() => _measurements.Single(_ => _.Instrument == WellKnownMetricNames.GrpcStreamsOpen).Value.ShouldEqual(1);

    double Value(string instrument, string grainType) => _measurements.Single(_ => _.Instrument == instrument && _.GrainType == grainType).Value;

    void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        string? grainType = null;
        string? eventStore = null;
        foreach (var tag in tags)
        {
            if (tag.Key == WellKnownTelemetryAttributes.GrainType) grainType = tag.Value as string;
            if (tag.Key == WellKnownTelemetryAttributes.EventStoreName) eventStore = tag.Value as string;
        }

        _measurements.Add((instrument.Name, value, grainType, eventStore));
    }
}
