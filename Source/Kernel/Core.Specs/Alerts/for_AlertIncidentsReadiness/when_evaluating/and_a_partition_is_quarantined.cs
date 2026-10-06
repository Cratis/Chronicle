// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReadiness.when_evaluating;

public class and_a_partition_is_quarantined : Specification
{
    AlertIncidentsReadinessState _result;

    void Because() => _result = AlertIncidentsReadiness.Evaluate(new ObserverState() with { RunningState = ObserverRunningState.Active }, new FailedPartitions { Partitions = [new FailedPartition { IsQuarantined = true }] }, EventSequenceNumber.Unavailable);

    [Fact] void should_report_degraded() => _result.ShouldEqual(AlertIncidentsReadinessState.Degraded);
}
