// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_the_observer_recovered : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _measurements;

    void Establish()
    {
        var entry = _gauges.Register(_observerKey, ObserverType.Reactor);
        entry.Publish(new(ObserverRunningState.Quarantined, 2, 1));
        Collect();
        entry.Publish(new(ObserverRunningState.Active, 0, 0));
        _clock.Advance(ObserverGauges.ZeroTail - TimeSpan.FromSeconds(1));
    }

    void Because() => _measurements = Collect();

    RecordedMeasurement Single(string instrument) => _measurements.Single(_ => _.Instrument == instrument);

    [Fact] void should_report_zero_failed_partitions() => Single("cratis.chronicle.observer.failed_partitions").Value.ShouldEqual(0);
    [Fact] void should_report_zero_quarantined_partitions() => Single("cratis.chronicle.observer.quarantined_partitions").Value.ShouldEqual(0);
    [Fact] void should_report_zero_for_the_quarantined_state() => Single("cratis.chronicle.observer.running_state").Value.ShouldEqual(0);
    [Fact] void should_report_it_for_the_state_it_left() => Single("cratis.chronicle.observer.running_state").TagMap["cratis.chronicle.observer.state"].ShouldEqual("quarantined");
}
