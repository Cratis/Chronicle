// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences.for_EventSequenceMetrics;

public class when_recording_append_duration : Specification
{
    static readonly string[] _tagKeys =
    [
        "cratis.event_store.name",
        "cratis.event_store.namespace",
        "cratis.event_sequence.id"
    ];

    readonly System.Diagnostics.Metrics.Meter _meter = new($"append-duration-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly List<(double Value, KeyValuePair<string, object?>[] Tags)> _recorded = [];
    Histogram<double>? _instrument;

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (!ReferenceEquals(instrument.Meter, _meter) || instrument.Name != WellKnownMetricNames.EventSequenceAppendDuration) return;
            _instrument = instrument as Histogram<double>;
            listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<double>((_, value, tags, _) => _recorded.Add((value, tags.ToArray())));
        _listener.Start();
    }

    void Because() =>
        new Meter<EventSequence>(_meter)
            .BeginEventSequenceScope("store", "namespace", MetricNames.Legacy)
            .AppendDuration(TimeSpan.FromMilliseconds(250), "event-log");

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_publish_the_histogram_in_every_naming_mode() => _instrument.ShouldNotBeNull();
    [Fact] void should_measure_in_seconds() => _instrument!.Unit.ShouldEqual("s");
    [Fact] void should_advise_the_explicit_buckets() => _instrument!.Advice!.HistogramBucketBoundaries!.ShouldEqual(EventSequenceMetrics.AppendDurationBuckets);
    [Fact] void should_record_the_duration_in_seconds() => _recorded.Single().Value.ShouldEqual(0.25);
    [Fact] void should_tag_with_exactly_the_sequence_dimensions() => _recorded.Single().Tags.Select(_ => _.Key).Order().SequenceEqual(_tagKeys.Order()).ShouldBeTrue();
    [Fact] void should_tag_the_event_sequence() => _recorded.Single().Tags.Single(_ => _.Key == "cratis.event_sequence.id").Value.ShouldEqual("event-log");
}
