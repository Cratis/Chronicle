// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

/// <summary>
/// Deleting a projection's stored definition is not enough on its own. The projections manager keeps its own
/// registered set, and its next activation subscribes everything in that set - which would recreate the very observer
/// records the removal just deleted. It has to be told to forget the projection as well.
/// </summary>
public class and_observer_is_a_projection : given.all_dependencies
{
    ProjectionId _projectionId;

    void Establish()
    {
        _projectionId = (ProjectionId)_observerId.Value;
        _projectionDefinitions.Has(_projectionId).Returns(true);
    }

    async Task Because() => await Remove();

    [Fact] async Task should_tell_the_projections_manager_to_forget_it() => await _projectionsManager.Received(1).Forget(_projectionId);
    [Fact] async Task should_delete_the_projection_definition() => await _projectionDefinitions.Received(1).Delete(_projectionId);
    [Fact] async Task should_delete_the_observer_definition() => await _observerDefinitions.Received(1).Delete(_observerId);
}
