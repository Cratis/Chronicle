// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_a_new_instance;

public class and_the_event_affects_only_children : given.a_set_initial_state_step
{
    ProjectionEventContext _context;

    void Establish() => _context = CreateContext(ProjectionOperationType.From | ProjectionOperationType.ChildrenAffected);

    async Task Because() => _context = await _step.Perform(_projection, _context);

    [Fact] void should_not_need_the_initial_state() => _context.NeedsInitialState.ShouldBeFalse();
}
