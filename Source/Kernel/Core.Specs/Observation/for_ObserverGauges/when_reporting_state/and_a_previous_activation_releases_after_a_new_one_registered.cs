// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_a_previous_activation_releases_after_a_new_one_registered : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _failed;

    void Establish()
    {
        var previous = _gauges.Register(_observerKey, ObserverType.Reactor);
        previous.Publish(new(ObserverRunningState.Active, 1, 0));
        _gauges.Register(_observerKey, ObserverType.Reactor).Publish(new(ObserverRunningState.Active, 5, 0));
        previous.Release();
    }

    void Because() => _failed = Collect("cratis.chronicle.observer.failed_partitions");

    [Fact] void should_keep_reporting_the_new_activation() => _failed.Single().Value.ShouldEqual(5);
}
