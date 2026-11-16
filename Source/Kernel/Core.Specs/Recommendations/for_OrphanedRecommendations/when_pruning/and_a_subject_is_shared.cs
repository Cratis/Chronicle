// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_a_subject_is_shared : given.a_pruner
{
    void Establish() => _rows.AddRange([Replay("shared"), Registration("shared", true)]);

    Task Because() => Prune();

    [Fact] async Task should_load_namespace_observer_state_once() => await _observers.Received(1).Get((ObserverId)"shared");
    [Fact] async Task should_check_observer_definitions_once() => await _store.Observers.Received(1).Has((ObserverId)"shared");
    [Fact] async Task should_check_projection_definitions_once() => await _store.Projections.Received(1).Has((ProjectionId)"shared");
    [Fact] async Task should_check_reactor_definitions_once() => await _store.Reactors.Received(1).Has((ReactorId)"shared");
    [Fact] async Task should_check_reducer_definitions_once() => await _store.Reducers.Received(1).Has((ReducerId)"shared");
    [Fact] async Task should_check_webhook_definitions_once() => await _store.Webhooks.Received(1).Has((WebhookId)"shared");
    [Fact] void should_remove_both_rows() => _count.ShouldEqual(2);
}
