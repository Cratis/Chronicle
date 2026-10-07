// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReadiness.when_evaluating;

public class and_the_observer_is_past_the_tail : Specification
{
    AlertIncidentsReadinessState _result;

    void Because() => _result = AlertIncidentsReadiness.Evaluate(new ObserverState() with { RunningState = ObserverRunningState.Active, NextEventSequenceNumber = 11UL }, new FailedPartitions(), 10UL);

    [Fact] void should_report_ready() => _result.ShouldEqual(AlertIncidentsReadinessState.Ready);
}
