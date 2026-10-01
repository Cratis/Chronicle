// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

public class and_the_retired_observer_is_quarantined : given.a_projections_manager_grain
{
    void Establish()
    {
        _state.Projections = [CreateDefinition("orphaned-projection", "orphaned-read-model")];
        _observerGrain.IsObserverQuarantined().Returns(true);
    }

    async Task Because() => await _grain.Register([], ProjectionOwner.Client);

    [Fact] async Task should_retire_the_observer() => await _observerGrain.Received(1).Retire();
    [Fact] async Task should_not_clear_the_observers_quarantine() => await _observerGrain.DidNotReceive().ClearObserverQuarantine();
    [Fact] async Task should_not_clear_failed_partitions_as_an_operator() => await _observerGrain.DidNotReceive().ClearFailedPartitions();
    [Fact] async Task should_not_unsubscribe_with_a_state_transition() => await _observerGrain.DidNotReceive().Unsubscribe();
    [Fact] async Task should_not_remove_the_observer() => await _observerGrain.DidNotReceive().Remove();
    [Fact]
    void should_retire_before_deleting_jobs_and_the_projection() => Received.InOrder(() =>
    {
        _observerGrain.Retire();
        _jobsManager.GetAllJobs();
        _projectionGrain.Remove();
    });
}
