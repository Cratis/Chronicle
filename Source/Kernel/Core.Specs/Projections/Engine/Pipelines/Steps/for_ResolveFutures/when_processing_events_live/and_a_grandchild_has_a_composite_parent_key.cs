// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_grandchild_has_a_composite_parent_key : given.a_resolve_futures_step
{
    readonly PropertyPath _parentPath = new("[children]");
    readonly PropertyPath _childPath = new("[children].[grandchildren]");
    ProjectionFuture _future;
    ExpandoObject _storedKey;
    ExpandoObject _state;
    ProjectionEventContext? _resolvedContext;
    bool _pendingBeforeParent;

    void Establish()
    {
        var child = Substitute.For<IProjection>();
        child.Parent.Returns(_projection);
        child.ChildrenPropertyPath.Returns(_parentPath);
        child.IdentifiedByProperty.Returns(new PropertyPath("id"));
        var grandchild = Substitute.For<IProjection>();
        grandchild.Parent.Returns(child);
        grandchild.Path.Returns(new ProjectionPath("children.grandchildren"));
        grandchild.ChildrenPropertyPath.Returns(_childPath);
        grandchild.OnNext(Arg.Do<ProjectionEventContext>(context =>
        {
            _resolvedContext = context;
            context.Changeset.AddChild<ExpandoObject>(_childPath, new PropertyPath("grandchildId"), "grandchild-key", [], context.Key.ArrayIndexers);
        }));
        child.ChildProjections.Returns([grandchild]);
        _projection.ChildProjections.Returns([child]);

        ((IDictionary<string, object?>)_event.Content)["grandchildId"] = "grandchild-key";
        var parentKey = new { tenant = "a", number = 42 }.AsExpandoObject();
        _future = new ProjectionFuture(
            ProjectionFutureId.New(),
            _projection.Identifier,
            _event,
            _parentPath,
            _childPath,
            new PropertyPath("grandchildId"),
            new PropertyPath("id"),
            new Key(parentKey, ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);
        _projectionFutures.GetFutures().Returns(Task.FromResult<IEnumerable<ProjectionFuture>>([_future]));
        _tracker.HasPending = true;
        _storedKey = new { number = 42, tenant = "a" }.AsExpandoObject();
        _state = new ExpandoObject();
    }

    async Task Because()
    {
        await _step.Perform(_projection, _context);
        _pendingBeforeParent = _resolvedContext is null;

        // A non-matching scalar identity must not prevent searching later composite identities.
        var otherParent = new { id = 42 }.AsExpandoObject();
        dynamic matchingParent = new ExpandoObject();
        matchingParent.id = _storedKey;
        matchingParent.grandchildren = new List<ExpandoObject>();
        ((IDictionary<string, object?>)_state)["children"] = new List<ExpandoObject> { otherParent, matchingParent };
        var parentEvent = AppendedEvent.EmptyWithEventType(new EventType("ChildAdded", EventTypeGeneration.First));
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), parentEvent, _state);
        await _step.Perform(_projection, _context with { Event = parentEvent, Changeset = changeset });
    }

    [Fact] void should_keep_the_future_pending_before_its_parent_arrives() => _pendingBeforeParent.ShouldBeTrue();
    [Fact] void should_apply_the_deferred_event() => _resolvedContext.ShouldNotBeNull();
    [Fact] void should_use_the_actual_stored_parent_key() => _resolvedContext!.Key.ArrayIndexers.All.First().Identifier.ShouldBeSame(_storedKey);
    [Fact] void should_materialize_the_grandchild() => ((IEnumerable<object>)_childPath.GetValue(_resolvedContext!.Changeset.CurrentState, _resolvedContext.Key.ArrayIndexers)!).Count().ShouldEqual(1);
    [Fact] void should_queue_the_future_for_save() => _context.PendingFutureSaves.Single().FutureId.ShouldEqual(_future.Id);
    [Fact] void should_keep_the_future_until_the_write_succeeds() => _projectionFutures.DidNotReceive().ResolveFuture(_future.Id);
}
