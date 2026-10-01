// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

public class and_quarantine_has_a_new_episode_identity : given.an_evaluator
{
    readonly Guid _newEpisode = Guid.NewGuid();
    ObserverAlertEvaluation _result;

    void Because() => _result = Evaluate(SnapshotOf() with { IsQuarantined = true, QuarantineEpisodeId = _newEpisode }, OpenQuarantineIncident());

    [Fact] void should_clear_the_old_episode_first() => ((AlertCleared)_result.Transitions[0]).IncidentId.Value.ShouldEqual(_defaultQuarantineEpisodeId);
    [Fact] void should_raise_the_source_owned_new_episode() => ((AlertRaised)_result.Transitions[1]).IncidentId.Value.ShouldEqual(_newEpisode);
}
