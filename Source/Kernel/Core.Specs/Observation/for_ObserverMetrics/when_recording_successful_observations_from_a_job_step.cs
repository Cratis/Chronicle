// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

public class when_recording_successful_observations_from_a_job_step : Specification
{
    static readonly string[] _tagKeys =
    [
        "cratis.observer.id",
        "cratis.event_store.name",
        "cratis.event_store.namespace",
        "cratis.event_sequence.id",
        "cratis.replay"
    ];

    readonly System.Diagnostics.Metrics.Meter _meter = new($"job-step-observations-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly List<string> _published = [];
    readonly List<KeyValuePair<string, object?>[]> _recorded = [];

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (!ReferenceEquals(instrument.Meter, _meter)) return;
            _published.Add(instrument.Name);
            listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((_, _, tags, _) => _recorded.Add(tags.ToArray()));
        _listener.Start();
    }

    void Because()
    {
        var metrics = new Meter<Observer>(_meter).BeginObservationScope(new("observer", "store", "namespace", "sequence"), MetricNames.Convention);
        metrics.SuccessfulObservation(replay: true);
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_create_only_the_successful_observations_instrument() => _published.ShouldContainOnly([WellKnownMetricNames.ObserverSuccessfulObservations]);
    [Fact] void should_record_the_observation() => _recorded.Count.ShouldEqual(1);
    [Fact] void should_tag_with_exactly_the_observer_dimensions_and_replay() => _recorded[0].Select(_ => _.Key).Order().SequenceEqual(_tagKeys.Order()).ShouldBeTrue();
    [Fact] void should_mark_it_as_replay() => _recorded[0].Single(_ => _.Key == "cratis.replay").Value.ShouldEqual(true);
}
