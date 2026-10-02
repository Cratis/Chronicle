// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.given;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_a_new_instance;

public class and_the_event_reaches_a_grandchild_only_as_an_event_property : given.a_set_initial_state_step
{
    ProjectionEventContext _context;

    async Task Establish()
    {
        var projection = await ProjectionWithChildOnlyEvents.Create();
        _projection.InitialModelState.Returns(projection.InitialModelState);
        _projection.TargetReadModelSchema.Returns(projection.TargetReadModelSchema);
        _context = CreateContext(projection.GetOperationTypeFor(ProjectionWithChildOnlyEvents.SubAttached));
    }

    async Task Because() => _context = await _step.Perform(_projection, _context);

    [Fact] void should_not_need_the_initial_state() => _context.NeedsInitialState.ShouldBeFalse();
}
