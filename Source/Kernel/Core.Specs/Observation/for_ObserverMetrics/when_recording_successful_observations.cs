// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

public class when_recording_successful_observations : Specification
{
    static readonly string[] _tagKeys =
    [
        "cratis.observer.id",
        "cratis.event_store.name",
        "cratis.event_store.namespace",
        "cratis.event_sequence.id",
        "cratis.replay"
    ];

    readonly System.Diagnostics.Metrics.Meter _meter = new($"successful-observations-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly List<KeyValuePair<string, object?>[]> _recorded = [];

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, _meter) && instrument.Name == WellKnownMetricNames.ObserverSuccessfulObservations) listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((_, _, tags, _) => _recorded.Add(tags.ToArray()));
        _listener.Start();
    }

    void Because()
    {
        var metrics = new Meter<Observer>(_meter).BeginObserverScope("observer", new("observer", "store", "namespace", "sequence"), MetricNames.Convention);
        metrics.SuccessfulObservation(replay: false);
        metrics.SuccessfulObservation(replay: true);
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_record_each_observation_once() => _recorded.Count.ShouldEqual(2);
    [Fact] void should_tag_with_exactly_the_observer_dimensions_and_replay() => _recorded.TrueForAll(_ => _.Select(tag => tag.Key).Order().SequenceEqual(_tagKeys.Order())).ShouldBeTrue();
    [Fact] void should_mark_a_live_observation_as_not_replay() => _recorded[0].Single(_ => _.Key == "cratis.replay").Value.ShouldEqual(false);
    [Fact] void should_mark_a_replayed_observation_as_replay() => _recorded[1].Single(_ => _.Key == "cratis.replay").Value.ShouldEqual(true);
}
