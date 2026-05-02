// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_HandleEvent;

public class when_root_key_is_unresolvable : given.a_handle_event_step
{
    ProjectionEventContext _result;

    void Establish() => _context.MarkKeyUnresolvable();

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_return_context() => _result.ShouldEqual(_context);
    [Fact] void should_not_call_on_next() => _projection.DidNotReceive().OnNext(Arg.Any<ProjectionEventContext>());
}
