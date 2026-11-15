// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

public class when_recording_successful_observations_with_legacy_names : Specification
{
    static readonly string[] _legacyTagKeys = ["ObserverId", "EventStore", "Namespace", "EventSequenceId"];

    readonly System.Diagnostics.Metrics.Meter _meter = new($"legacy-successful-observations-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly List<KeyValuePair<string, object?>[]> _recorded = [];

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, _meter) && instrument.Name == "chronicle-observer-successful-observations") listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((_, _, tags, _) => _recorded.Add(tags.ToArray()));
        _listener.Start();
    }

    void Because() =>
        new Meter<Observer>(_meter)
            .BeginObserverScope("observer", new("observer", "store", "namespace", "sequence"), MetricNames.Legacy)
            .SuccessfulObservation(replay: true);

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_record_the_observation() => _recorded.Count.ShouldEqual(1);
    [Fact] void should_keep_exactly_the_legacy_dimensions() => _recorded[0].Select(_ => _.Key).Order().SequenceEqual(_legacyTagKeys.Order()).ShouldBeTrue();
}
