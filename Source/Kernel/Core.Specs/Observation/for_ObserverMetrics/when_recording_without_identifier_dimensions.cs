// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.EventSequences;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

public class when_recording_without_identifier_dimensions : Specification
{
    readonly string _namespace = $"metric-ids-{Guid.NewGuid():N}";
    readonly List<(string Name, KeyValuePair<string, object?>[] Tags)> _measurements = [];
    System.Diagnostics.Metrics.Meter _meter;
    MeterListener _listener;

    void Establish()
    {
        _meter = new("identifier-dimension-specs");
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) => listener.EnableMeasurementEvents(instrument)
        };
        _listener.SetMeasurementEventCallback<int>((instrument, _, tags, _) =>
        {
            var copied = tags.ToArray();
            if (copied.Any(tag => tag.Key == "Namespace" && tag.Value?.ToString() == _namespace) ||
                instrument.Name == "chronicle-connected-clients")
            {
                lock (_measurements) _measurements.Add((instrument.Name, copied));
            }
        });
        _listener.Start();
    }

    void Because()
    {
        var events = new Meter<EventSequence>(_meter).BeginEventSequenceScope("store", _namespace);
        for (var index = 0; index < 100; index++)
        {
            events.AppendedEvent("Event");
            events.DuplicateEventSequenceNumber("Event");
            events.FailedAppending("Event");
            events.ConstraintViolation("Event");
        }

        var observer = new Meter<Observer>(_meter).BeginScope(new Dictionary<string, object>
        {
            ["Namespace"] = _namespace,
            ["ObserverId"] = "observer",
            ["Partition"] = "private-partition",
            ["EventSourceId"] = "customer-42"
        });
        observer.PartitionFailed();
        observer.PartitionRetryAttempt();
        observer.PartitionQuarantined();
    }

    [Fact] void should_record_all_append_measurements() => _measurements.Count(_ => _.Name.StartsWith("chronicle-event-sequences-", StringComparison.Ordinal)).ShouldEqual(400);
    [Fact] void should_keep_one_series_per_append_counter() => _measurements.Where(_ => _.Name.StartsWith("chronicle-event-sequences-", StringComparison.Ordinal)).GroupBy(_ => _.Name).All(group => group.Select(_ => string.Join(';', _.Tags.Select(tag => $"{tag.Key}={tag.Value}"))).Distinct().Count() == 1).ShouldBeTrue();
    [Fact] void should_not_attach_an_event_source_id() => _measurements.SelectMany(_ => _.Tags).Any(_ => _.Key.Equals("EventSourceId", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_not_attach_a_partition() => _measurements.SelectMany(_ => _.Tags).Any(_ => _.Key.Equals("Partition", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_keep_the_observer_dimension() => _measurements.Exists(_ => _.Tags.Any(tag => tag.Key == "ObserverId" && Equals(tag.Value, "observer"))).ShouldBeTrue();
    [Fact] void should_record_all_failure_counters() => _measurements.Count(_ => _.Name.StartsWith("chronicle-observer-", StringComparison.Ordinal)).ShouldEqual(3);

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }
}
