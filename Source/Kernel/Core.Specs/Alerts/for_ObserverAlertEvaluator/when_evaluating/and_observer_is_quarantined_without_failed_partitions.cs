// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// An observer can be quarantined with no failed partition, for example when a catch-up job could not be started. It
/// still raises, with no attempts to report.
/// </summary>
public class and_observer_is_quarantined_without_failed_partitions : given.an_evaluator
{
    ObserverAlertEvaluation _result;
    AlertRaised _raised;

    void Because()
    {
        _result = Evaluate(SnapshotOf() with { IsQuarantined = true });
        _raised = _result.Transitions.OfType<AlertRaised>().Single();
    }

    [Fact] void should_only_raise() => _result.Transitions.Count.ShouldEqual(1);
    [Fact] void should_raise_the_observer_quarantined_condition() => _raised.Condition.ShouldEqual(AlertConditionKind.ObserverQuarantined);
    [Fact] void should_report_no_attempts() => _raised.Evidence.AttemptCount.ShouldEqual(0);
    [Fact] void should_not_classify_the_failure() => _raised.Evidence.FailureKind.ShouldEqual(FailureKind.Unknown);
}
