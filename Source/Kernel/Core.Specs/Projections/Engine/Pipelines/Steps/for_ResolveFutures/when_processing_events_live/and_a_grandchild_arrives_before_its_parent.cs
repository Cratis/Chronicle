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
    readonly List<string> _projected = [];
    ProjectionFuture _future;
    IProjection _child;
    IProjection _grandchild;
    ProjectionEventContext? _resolvedContext;
    bool _resolved;
    bool _resolvedBeforeParent;

    void Establish()
    {
        _child = Substitute.For<IProjection>();
        _child.Parent.Returns(_projection);
        _child.ChildrenPropertyPath.Returns(new PropertyPath("children"));
        _child.IdentifiedByProperty.Returns(new PropertyPath("childId"));
        _grandchild = Substitute.For<IProjection>();
        _grandchild.Parent.Returns(_child);
        _grandchild.Path.Returns(new ProjectionPath("children.grandchildren"));
        _grandchild.ChildrenPropertyPath.Returns(new PropertyPath("children.grandchildren"));
        _grandchild.OnNext(Arg.Do<ProjectionEventContext>(context =>
        {
            _resolvedContext = context;
            _projected.Add("grandchild");
        }));
        _projection.OnDescendantEvent(Arg.Do<ProjectionEventContext>(_ => _projected.Add("root")));
        _child.OnDescendantEvent(Arg.Do<ProjectionEventContext>(_ => _projected.Add("child")));
        _child.ChildProjections.Returns([_grandchild]);
        _projection.ChildProjections.Returns([_child]);

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
    [Fact] void should_apply_the_ancestors_descendant_event_mappings_before_projecting_the_grandchild() => _projected.ShouldEqual<IEnumerable<string>>(["root", "child", "grandchild"]);
    [Fact] void should_give_the_root_the_resolved_key() => _projection.Received(1).OnDescendantEvent(Arg.Is<ProjectionEventContext>(_ => _.Key == _resolvedContext!.Key && _.Changeset == _resolvedContext.Changeset));
    [Fact] void should_give_the_child_the_resolved_key() => _child.Received(1).OnDescendantEvent(Arg.Is<ProjectionEventContext>(_ => _.Key == _resolvedContext!.Key && _.Changeset == _resolvedContext.Changeset));
    [Fact] void should_not_give_the_grandchild_a_descendant_event() => _grandchild.DidNotReceive().OnDescendantEvent(Arg.Any<ProjectionEventContext>());
}
