// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

public class and_quarantined_observer_retirement_fails : given.a_projections_manager_grain
{
    Exception _error;

    void Establish()
    {
        _state.Projections = [CreateDefinition("orphaned-projection", "orphaned-read-model")];
        _observerGrain.IsObserverQuarantined().Returns(true);
        _observerGrain.Retire().Returns(Task.FromException(new InvalidOperationException("Unable to retire quarantined observer")));
    }

    async Task Because() => _error = await Catch.Exception(() => _grain.Register([], ProjectionOwner.Client));

    [Fact] void should_report_the_failed_retirement() => _error.ShouldBeOfExactType<SomeProjectionDefinitionsFailedToRegister>();
    [Fact] void should_keep_the_projection_registered_for_a_retry() => _state.Projections.Single().Identifier.ShouldEqual((ProjectionId)"orphaned-projection");
    [Fact] async Task should_not_delete_jobs() => await _jobsManager.DidNotReceive().GetAllJobs();
    [Fact] async Task should_not_remove_the_projection() => await _projectionGrain.DidNotReceive().Remove();
}
