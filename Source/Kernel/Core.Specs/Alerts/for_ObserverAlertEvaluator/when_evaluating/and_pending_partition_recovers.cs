// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// A partition that recovered before its raise fell due leaves nothing behind: no raise, no clear, and no raise left
/// to come back for.
/// </summary>
public class and_pending_partition_recovers : given.an_evaluator
{
    ObserverAlertEvaluation _whileFailing;
    ObserverAlertEvaluation _result;

    void Because()
    {
        _whileFailing = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(2))));
        _result = Evaluate(SnapshotOf());
    }

    [Fact] void should_have_been_waiting_for_the_raise_while_it_was_failing() => _whileFailing.NextRaiseDue.ShouldNotBeNull();
    [Fact] void should_not_transition() => _result.Transitions.ShouldBeEmpty();
    [Fact] void should_no_longer_wait_for_anything() => _result.NextRaiseDue.ShouldBeNull();
}
