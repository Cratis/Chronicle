// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_HandleEvent;

public class when_child_key_is_unresolvable : given.a_handle_event_step
{
    IProjection _childProjection;
    ProjectionEventContext _result;

    void Establish()
    {
        _childProjection = Substitute.For<IProjection>();
        _childProjection.Path.Returns(new ProjectionPath("Items"));
        _childProjection.Identifier.Returns(new ProjectionId("items-child"));
        _childProjection.ChildrenPropertyPath.Returns(new PropertyPath("Items"));
        _childProjection.ChildProjections.Returns([]);
        _childProjection.HasKeyResolverFor(_event.Context.EventType).Returns(true);
        _childProjection.GetKeyResolverFor(_event.Context.EventType).Returns(
            (_, _, _) => Task.FromResult(KeyResolverResult.Unresolvable()));

        _projection.Accepts(_event.Context.EventType).Returns(false);
        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);
        _projection.ChildProjections.Returns([_childProjection]);
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_return_context() => _result.ShouldEqual(_context);
    [Fact] void should_not_add_any_deferred_futures() => _result.DeferredFutures.ShouldBeEmpty();
    [Fact] void should_not_call_child_on_next() => _childProjection.DidNotReceive().OnNext(Arg.Any<ProjectionEventContext>());
}
