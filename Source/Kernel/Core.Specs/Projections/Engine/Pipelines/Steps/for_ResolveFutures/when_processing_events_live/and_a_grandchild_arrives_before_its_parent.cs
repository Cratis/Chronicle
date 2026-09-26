// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_grandchild_arrives_before_its_parent : given.a_resolve_futures_step
{
    ProjectionFuture _future;
    ProjectionEventContext? _resolvedContext;
    bool _resolved;
    bool _resolvedBeforeParent;

    void Establish()
    {
        var child = Substitute.For<IProjection>();
        child.Parent.Returns(_projection);
        child.ChildrenPropertyPath.Returns(new PropertyPath("children"));
        child.IdentifiedByProperty.Returns(new PropertyPath("childId"));
        var grandchild = Substitute.For<IProjection>();
        grandchild.Parent.Returns(child);
        grandchild.Path.Returns(new ProjectionPath("children.grandchildren"));
        grandchild.ChildrenPropertyPath.Returns(new PropertyPath("children.grandchildren"));
        grandchild.OnNext(Arg.Do<ProjectionEventContext>(context => _resolvedContext = context));
        child.ChildProjections.Returns([grandchild]);
        _projection.ChildProjections.Returns([child]);

        _future = new ProjectionFuture(
            ProjectionFutureId.New(),
            _projection.Identifier,
            _event,
            new PropertyPath("children"),
            new PropertyPath("children.grandchildren"),
            new PropertyPath("grandchildId"),
            new PropertyPath("childId"),
            new Key("parent-key", ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);
        _projectionFutures.GetFutures().Returns(_ => Task.FromResult<IEnumerable<ProjectionFuture>>(_resolved ? [] : [_future]));
        _projectionFutures.ResolveFuture(_future.Id).Returns(_ =>
        {
            _resolved = true;
            return Task.CompletedTask;
        });
        _tracker.HasPending = true;
    }

    async Task Because()
    {
        // The grandchild precedes its child parent in the live stream.
        await _step.Perform(_projection, _context);
        _resolvedBeforeParent = _resolved;

        dynamic parent = new ExpandoObject();
        parent.childId = "parent-key";
        parent.grandchildren = new List<ExpandoObject>();
        dynamic root = new ExpandoObject();
        root.children = new List<ExpandoObject> { parent };
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.CurrentState.Returns((ExpandoObject)root);
        var parentEvent = AppendedEvent.EmptyWithEventType(new EventType("ChildAdded", EventTypeGeneration.First));
        await _step.Perform(_projection, _context with { Event = parentEvent, Changeset = changeset });
    }

    [Fact] void should_leave_the_future_pending_before_its_parent_arrives() => _resolvedBeforeParent.ShouldBeFalse();
    [Fact] void should_resolve_the_future_after_the_child_parent_arrives() => _projectionFutures.Received(1).ResolveFuture(_future.Id);
    [Fact] void should_index_the_parent_and_grandchild() => _resolvedContext!.Key.ArrayIndexers.All.Count().ShouldEqual(2);
    [Fact] void should_use_the_matching_parent_key() => _resolvedContext!.Key.ArrayIndexers.All.First().Identifier.ShouldEqual("parent-key");
    [Fact] void should_leave_no_future_pending() => _tracker.HasPending.ShouldBeFalse();
}
