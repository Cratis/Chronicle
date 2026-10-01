// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_revival_was_not_reported_before_requarantine : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(
        SnapshotOf() with
        {
            IsQuarantined = true,
            QuarantineEpisodeId = Guid.NewGuid(),
            Endings = new Dictionary<IncidentId, AlertClearedReason> { [new(_defaultQuarantineEpisodeId)] = AlertClearedReason.Revived }
        },
        OpenQuarantineIncident());

    [Fact] void should_end_the_identified_previous_episode_as_revived() => ((AlertCleared)_result.Transitions[0]).Reason.ShouldEqual(AlertClearedReason.Revived);
    [Fact] void should_raise_the_new_episode_without_a_healthy_snapshot() => _result.Transitions[1].ShouldBeOfExactType<AlertRaised>();
}
