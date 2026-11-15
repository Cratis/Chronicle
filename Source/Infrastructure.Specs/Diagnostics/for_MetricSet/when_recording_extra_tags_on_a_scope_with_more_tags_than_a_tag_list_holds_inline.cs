// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.for_MetricSet;

/// <summary>
/// A TagList holds eight tags inline and spills the rest into an array, which a copy of the TagList shares.
/// The nested recording reuses the spilled slot the outer measurement is still reading, so a shared array shows up as
/// the outer measurement's extra tag changing underneath it.
/// </summary>
public class when_recording_extra_tags_on_a_scope_with_more_tags_than_a_tag_list_holds_inline : Specification
{
    const string OuterValue = "outer";
    const string NestedValue = "nested";

    static readonly KeyValuePair<string, object?>[] _scopeTags = [.. Enumerable.Range(0, 9).Select(_ => new KeyValuePair<string, object?>($"scope.{_}", _))];

    readonly List<(string Name, int Value, KeyValuePair<string, object?>[] Tags)> _measurements = [];
    readonly MeterListener _listener = new();
    Meter _meter;
    MetricInstrument<int, CounterMetricRecorder<int>> _appended;
    MetricInstrument<int, CounterMetricRecorder<int>> _failed;
    KeyValuePair<string, object?>[] _outerTagsAfterNestedRecord = [];

    void Establish()
    {
        _meter = new($"spilled-scope-tags-{Guid.NewGuid():N}");
        var metrics = new MetricSet(_meter, MetricNames.Convention, _scopeTags);
        _appended = metrics.Counter<int>(WellKnownMetricNames.EventSequenceAppendedEvents, WellKnownMetricNames.EventSequenceAppendedEventsUnit, firstKey: WellKnownTelemetryAttributes.EventTypeId);
        _failed = metrics.Counter<int>(WellKnownMetricNames.ObserverPartitionFailures, WellKnownMetricNames.ObserverPartitionFailuresUnit);

        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter == _meter) listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) =>
        {
            if (value == 1)
            {
                _appended.Record(2, NestedValue);
                _failed.Record(3);
                _outerTagsAfterNestedRecord = tags.ToArray();
            }
            _measurements.Add((instrument.Name, value, tags.ToArray()));
        });
        _listener.Start();
    }

    void Because() => _appended.Record(1, OuterValue);

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_keep_the_outer_extra_tag_while_another_record_appends_its_own() => _outerTagsAfterNestedRecord.ShouldEqual([.. _scopeTags, new(WellKnownTelemetryAttributes.EventTypeId, OuterValue)]);
    [Fact] void should_record_the_nested_extra_tag_after_the_scope_tags() => _measurements.Single(_ => _.Value == 2).Tags.ShouldEqual([.. _scopeTags, new(WellKnownTelemetryAttributes.EventTypeId, NestedValue)]);
    [Fact] void should_record_only_the_scope_tags_on_the_other_instrument() => _measurements.Single(_ => _.Value == 3).Tags.ShouldEqual(_scopeTags);
}
