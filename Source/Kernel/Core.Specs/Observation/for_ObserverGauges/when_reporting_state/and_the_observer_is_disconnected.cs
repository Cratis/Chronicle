// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_the_observer_is_disconnected : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _runningState;

    void Establish() => _gauges.Register(_observerKey, ObserverType.Reactor).Publish(new(ObserverRunningState.Disconnected, 0, 0));

    void Because() => _runningState = Collect("cratis.chronicle.observer.running_state");

    [Fact] void should_not_report_the_running_state_per_observer() => _runningState.ShouldBeEmpty();
}
