// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_observer_is_not_a_projection : given.all_dependencies
{
    async Task Because() => await Remove();

    [Fact] async Task should_not_ask_the_projections_manager_to_forget_anything() => await _projectionsManager.DidNotReceive().Forget(Arg.Any<ProjectionId>());
    [Fact] async Task should_not_delete_a_projection_definition() => await _projectionDefinitions.DidNotReceive().Delete(Arg.Any<ProjectionId>());
}
