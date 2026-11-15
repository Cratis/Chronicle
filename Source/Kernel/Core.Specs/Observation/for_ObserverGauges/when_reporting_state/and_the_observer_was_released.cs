// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_the_observer_was_released : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _failed;

    void Establish()
    {
        var entry = _gauges.Register(_observerKey, ObserverType.Reactor);
        entry.Publish(new(ObserverRunningState.Active, 1, 0));
        Collect();
        entry.Release();
        _clock.Advance(TimeSpan.FromMinutes(1));
    }

    void Because() => _failed = Collect("cratis.chronicle.observer.failed_partitions");

    [Fact] void should_report_zero_for_the_failed_partitions() => _failed.Single().Value.ShouldEqual(0);
    [Fact] void should_report_it_for_the_observer() => _failed.Single().TagMap["cratis.observer.id"].ShouldEqual("observer");
}
