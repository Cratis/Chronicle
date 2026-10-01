// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// The message in the evidence is cut to 200 characters, so a long exception message does not end up in the event log
/// in full.
/// </summary>
public class and_failure_message_is_long : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(10), message: new string('x', 500))));

    [Fact] void should_cut_the_message() => _result.Transitions.OfType<AlertRaised>().Single().Evidence.Message.Length.ShouldEqual(AlertEvidence.MaxMessageLength);
}
