// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_the_observer_recovered_before_a_collector_slower_than_the_zero_tail : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _first;
    IReadOnlyList<RecordedMeasurement> _second;

    void Establish()
    {
        var entry = _gauges.Register(_observerKey, ObserverType.Reactor);
        entry.Publish(new(ObserverRunningState.Quarantined, 2, 1));
        Collect();
        entry.Publish(new(ObserverRunningState.Active, 0, 0));
        _clock.Advance(ObserverGauges.ZeroTail + TimeSpan.FromMinutes(1));
    }

    void Because()
    {
        _first = [.. Collect().Where(_ => _.TagMap.ContainsKey("cratis.observer.id"))];
        _clock.Advance(ObserverGauges.ZeroTail + TimeSpan.FromMinutes(1));
        _second = [.. Collect().Where(_ => _.TagMap.ContainsKey("cratis.observer.id"))];
    }

    [Fact] void should_report_zero_failed_partitions_once() => _first.Single(_ => _.Instrument == "cratis.chronicle.observer.failed_partitions").Value.ShouldEqual(0);
    [Fact] void should_report_zero_quarantined_partitions_once() => _first.Single(_ => _.Instrument == "cratis.chronicle.observer.quarantined_partitions").Value.ShouldEqual(0);
    [Fact] void should_report_zero_for_the_quarantined_state_once() => _first.Single(_ => _.Instrument == "cratis.chronicle.observer.running_state").Value.ShouldEqual(0);
    [Fact] void should_stop_reporting_the_observer_afterwards() => _second.ShouldBeEmpty();
}
