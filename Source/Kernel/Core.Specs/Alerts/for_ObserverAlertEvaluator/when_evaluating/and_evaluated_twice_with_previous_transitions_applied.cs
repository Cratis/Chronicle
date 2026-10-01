// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// Evaluating the same snapshot again, with the transitions of the first evaluation applied to the open incidents,
/// yields nothing new, at every step of the life of an incident: raised, escalated and cleared. That is what makes a
/// duplicate notification or a restart harmless.
/// </summary>
public class and_evaluated_twice_with_previous_transitions_applied : given.an_evaluator
{
    readonly FailedPartitionId _id = FailedPartitionId.New();
    IReadOnlyCollection<OpenIncident> _open = [];
    ObserverAlertEvaluation _firstRaise;
    ObserverAlertEvaluation _secondRaise;
    ObserverAlertEvaluation _firstEscalation;
    ObserverAlertEvaluation _secondEscalation;
    ObserverAlertEvaluation _firstClear;
    ObserverAlertEvaluation _secondClear;

    void Because()
    {
        var failing = SnapshotOf(FailedPartition(_id, TimeSpan.FromMinutes(10))) with { IsQuarantined = true };
        _firstRaise = _evaluator.Evaluate(failing, _open, _now);
        _open = _firstRaise.ApplyTo(_open);
        _secondRaise = _evaluator.Evaluate(failing, _open, _now);

        var exhausted = SnapshotOf(FailedPartition(_id, TimeSpan.FromMinutes(30), isQuarantined: true)) with { IsQuarantined = true };
        _firstEscalation = _evaluator.Evaluate(exhausted, _open, _now);
        _open = _firstEscalation.ApplyTo(_open);
        _secondEscalation = _evaluator.Evaluate(exhausted, _open, _now);

        var recovered = SnapshotOf();
        _firstClear = _evaluator.Evaluate(recovered, _open, _now);
        _open = _firstClear.ApplyTo(_open);
        _secondClear = _evaluator.Evaluate(recovered, _open, _now);
    }

    [Fact] void should_raise_the_partition_and_the_quarantine_the_first_time() => _firstRaise.Transitions.OfType<AlertRaised>().Select(_ => _.Condition).ShouldContainOnly(AlertConditionKind.PartitionFailing, AlertConditionKind.ObserverQuarantined);
    [Fact] void should_not_raise_again() => _secondRaise.Transitions.ShouldBeEmpty();
    [Fact] void should_escalate_the_first_time() => _firstEscalation.Transitions.OfType<AlertEscalated>().Count().ShouldEqual(1);
    [Fact] void should_not_escalate_again() => _secondEscalation.Transitions.ShouldBeEmpty();
    [Fact] void should_clear_the_partition_and_the_quarantine_the_first_time() => _firstClear.Transitions.OfType<AlertCleared>().Count().ShouldEqual(2);
    [Fact] void should_not_clear_again() => _secondClear.Transitions.ShouldBeEmpty();
    [Fact] void should_have_nothing_left_open() => _open.ShouldBeEmpty();
}
