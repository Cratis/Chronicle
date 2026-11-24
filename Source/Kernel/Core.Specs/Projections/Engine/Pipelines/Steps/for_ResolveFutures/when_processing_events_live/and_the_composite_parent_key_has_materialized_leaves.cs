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

public class and_the_composite_parent_key_has_materialized_leaves : given.a_resolve_futures_step
{
    readonly PropertyPath _parentPath = "[children]";
    readonly PropertyPath _childPath = "[children].[grandchildren]";
    ProjectionFuture _future;
    ExpandoObject _storedKey;
    ProjectionEventContext? _resolvedContext;

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

        var guid = Guid.Parse("ad037c15-1354-4cd7-a069-f24aa1706109");
        var lookup = new { Tenant = "tenant", Number = 7, Id = guid }.AsExpandoObject();
        _storedKey = new { tenant = "tenant", number = 7L, id = guid.ToString().ToUpperInvariant() }.AsExpandoObject();
        var ambiguous = new ExpandoObject();
        var ambiguousProperties = (IDictionary<string, object?>)ambiguous;
        ambiguousProperties["tenant"] = "tenant";
        ambiguousProperties["Tenant"] = "tenant";
        ambiguousProperties["number"] = 7;
        ambiguousProperties["id"] = guid;
        List<ExpandoObject> siblings =
        [
            Parent(42),
            Parent(new { tenant = "tenant" }.AsExpandoObject()),
            Parent(new { tenant = "tenant", number = 8L, id = guid.ToString() }.AsExpandoObject()),
            Parent(ambiguous)
        ];
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["children"] = siblings.Append(Parent(_storedKey)).ToList();
        _context = _context with { Changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), _event, state) };
        ((IDictionary<string, object?>)_event.Content)["grandchildId"] = "grandchild-key";
        _future = new(ProjectionFutureId.New(), _projection.Identifier, _event, _parentPath, _childPath, new PropertyPath("grandchildId"), new PropertyPath("id"), new Key(lookup, ArrayIndexers.NoIndexers), DateTimeOffset.UtcNow);
        _projectionFutures.GetFutures().Returns(Task.FromResult<IEnumerable<ProjectionFuture>>([_future]));
        _tracker.HasPending = true;
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_resolve_the_future() => _resolvedContext.ShouldNotBeNull();
    [Fact] void should_reuse_the_actual_stored_identity() => _resolvedContext!.Key.ArrayIndexers.All.First().Identifier.ShouldBeSame(_storedKey);
    [Fact] void should_add_exactly_one_grandchild() => ((IEnumerable<object>)_childPath.GetValue(_resolvedContext!.Changeset.CurrentState, _resolvedContext.Key.ArrayIndexers)!).Count().ShouldEqual(1);
    [Fact] void should_leave_incompatible_and_partial_siblings_untouched() => SiblingGrandchildCounts().ShouldEachConformTo(count => count == 0);
    [Fact] void should_queue_exactly_one_save() => _context.PendingFutureSaves.Select(save => save.FutureId).ShouldEqual(_future.Id);
    [Fact] void should_not_resolve_before_the_save() => _projectionFutures.DidNotReceive().ResolveFuture(_future.Id);

    IEnumerable<int> SiblingGrandchildCounts() =>
        ((IEnumerable<object>)((IDictionary<string, object?>)_resolvedContext!.Changeset.CurrentState)["children"]!)
            .Take(4).Cast<IDictionary<string, object?>>()
            .Select(parent => ((IEnumerable<object>)parent["grandchildren"]!).Count());

    static ExpandoObject Parent(object key)
    {
        var parent = new ExpandoObject();
        ((IDictionary<string, object?>)parent)["id"] = key;
        ((IDictionary<string, object?>)parent)["grandchildren"] = new List<ExpandoObject>();
        return parent;
    }
}
