// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_the_observer_is_healthy : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _measurements;

    void Establish() => _gauges.Register(_observerKey, ObserverType.Reactor).Publish(new(ObserverRunningState.Active, 0, 0));

    void Because() => _measurements = Collect();

    [Fact] void should_not_report_failed_partitions() => _measurements.Any(_ => _.Instrument == "cratis.chronicle.observer.failed_partitions").ShouldBeFalse();
    [Fact] void should_not_report_quarantined_partitions() => _measurements.Any(_ => _.Instrument == "cratis.chronicle.observer.quarantined_partitions").ShouldBeFalse();
    [Fact] void should_not_report_the_running_state() => _measurements.Any(_ => _.Instrument == "cratis.chronicle.observer.running_state").ShouldBeFalse();
    [Fact] void should_count_it_as_resident() => _measurements.Single(_ => _.Instrument == "cratis.chronicle.observer.resident").Value.ShouldEqual(1);
}
