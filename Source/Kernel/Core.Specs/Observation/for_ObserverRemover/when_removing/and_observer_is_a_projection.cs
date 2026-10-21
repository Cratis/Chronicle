// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

/// <summary>
/// Projection removal must use the manager's live-client guard rather than the kernel subscription.
/// </summary>
public class and_observer_is_a_projection : given.all_dependencies
{
    ProjectionId _projectionId;
    ObserverRemovalResult _result;

    void Establish()
    {
        _projectionId = (ProjectionId)_observerId.Value;
        _projectionDefinitions.Has(_projectionId).Returns(true);
        ObserverIs(_observerInFirstNamespace, ObserverRunningState.Active, true);
        _projectionsManager.Remove(_projectionId).Returns(ObserverRemovalResult.Removed);
    }

    async Task Because() => _result = await Remove();

    [Fact] void should_remove_the_orphan_despite_its_kernel_subscription() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);

    [Fact] async Task should_remove_through_the_projections_manager() => await _projectionsManager.Received(1).Remove(_projectionId);
    [Fact] async Task should_leave_teardown_to_the_manager() => await _observerInFirstNamespace.DidNotReceive().Remove();
}
