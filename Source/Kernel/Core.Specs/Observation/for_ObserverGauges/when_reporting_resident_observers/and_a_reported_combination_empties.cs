// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_resident_observers;

public class and_a_reported_combination_empties : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _resident;

    void Establish()
    {
        var entry = _gauges.Register(_observerKey, ObserverType.Reactor);
        entry.Publish(new(ObserverRunningState.Active, 0, 0));
        Collect();
        entry.Publish(new(ObserverRunningState.Quarantined, 0, 0));
    }

    void Because() => _resident = Collect("cratis.chronicle.observer.resident");

    long For(string state) => _resident.Single(_ => (string)_.TagMap["cratis.chronicle.observer.state"]! == state).Value;

    [Fact] void should_report_zero_for_the_combination_it_left() => For("active").ShouldEqual(0);
    [Fact] void should_count_it_in_its_new_combination() => For("quarantined").ShouldEqual(1);
}
