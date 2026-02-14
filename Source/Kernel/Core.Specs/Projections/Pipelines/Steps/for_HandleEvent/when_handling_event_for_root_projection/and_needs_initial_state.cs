// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Pipelines.Steps.for_HandleEvent.when_handling_event_for_root_projection;

public class and_needs_initial_state : given.a_handle_event_step
{
    ProjectionEventContext _result;
    int _addCallCount;

    void Establish()
    {
        dynamic initialState = _projectionInitialModelState;
        initialState.Configurations = new List<object>();
        initialState.SomeProperty = "test value";

        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);
        _projection.Accepts(_event.Context.EventType).Returns(true);

        _changeset.When(_ => _.Add(Arg.Any<Change>()))
            .Do(_ => _addCallCount++);

        _context = _context with { NeedsInitialState = true };
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_add_changes_to_changeset() => _addCallCount.ShouldBeGreaterThan(0);
    [Fact] void should_return_context() => _result.ShouldEqual(_context);
}
