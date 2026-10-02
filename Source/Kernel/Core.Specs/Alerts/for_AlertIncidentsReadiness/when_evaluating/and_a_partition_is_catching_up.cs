// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReadiness.when_evaluating;

public class and_a_partition_is_catching_up : Specification
{
    AlertIncidentsReadinessState _result;

    void Because() => _result = AlertIncidentsReadiness.Evaluate(new ObserverState() with { RunningState = ObserverRunningState.Active, CatchingUpPartitions = new HashSet<Key> { Key.Undefined } }, new FailedPartitions(), EventSequenceNumber.Unavailable);

    [Fact] void should_report_catchingup() => _result.ShouldEqual(AlertIncidentsReadinessState.CatchingUp);
}
