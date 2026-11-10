// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_the_observer_was_released : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _measurements;

    void Establish()
    {
        var entry = _gauges.Register(_observerKey, ObserverType.Reactor);
        entry.Publish(new(ObserverRunningState.Active, 1, 0));
        entry.Release();
    }

    void Because() => _measurements = Collect();

    [Fact] void should_report_nothing_for_the_observer() => _measurements.ShouldBeEmpty();
}
