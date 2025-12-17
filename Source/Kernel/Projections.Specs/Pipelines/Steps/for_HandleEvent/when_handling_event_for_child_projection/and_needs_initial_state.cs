// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Pipelines.Steps.for_HandleEvent.when_handling_event_for_child_projection;

public class and_needs_initial_state : given.a_handle_event_step
{
    ProjectionEventContext _result;

    void Establish()
    {
        dynamic initialState = _projectionInitialModelState;
        initialState.Hubs = new List<object>();
        initialState.AnotherProperty = "child value";

        _projection.ChildrenPropertyPath.Returns(new PropertyPath("Configurations"));
        _projection.Accepts(_event.Context.EventType).Returns(true);

        _context = _context with { NeedsInitialState = true };
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_not_add_to_changeset() => _changeset.DidNotReceive().Add(Arg.Any<Change>());
    [Fact] void should_return_context() => _result.ShouldEqual(_context);
}
