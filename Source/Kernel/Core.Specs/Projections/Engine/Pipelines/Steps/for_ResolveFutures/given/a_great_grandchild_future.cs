// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.given;

public class a_great_grandchild_future : a_resolve_futures_step
{
    protected ProjectionFuture _future;
    protected ProjectionEventContext? _resolvedContext;
    protected bool _resolved;

    void Establish()
    {
        var child = Substitute.For<IProjection>();
        child.Parent.Returns(_projection);
        child.ChildrenPropertyPath.Returns(new PropertyPath("children"));
        child.IdentifiedByProperty.Returns(new PropertyPath("childId"));

        var grandchild = Substitute.For<IProjection>();
        grandchild.Parent.Returns(child);
        grandchild.ChildrenPropertyPath.Returns(new PropertyPath("children.grandchildren"));
        grandchild.IdentifiedByProperty.Returns(new PropertyPath("grandchildId"));

        var greatGrandchild = Substitute.For<IProjection>();
        greatGrandchild.Parent.Returns(grandchild);
        greatGrandchild.Path.Returns(new ProjectionPath("children.grandchildren.items"));
        greatGrandchild.ChildrenPropertyPath.Returns(new PropertyPath("children.grandchildren.items"));
        greatGrandchild.ChildProjections.Returns([]);
        greatGrandchild.OnNext(Arg.Do<ProjectionEventContext>(context => _resolvedContext = context));

        grandchild.ChildProjections.Returns([greatGrandchild]);
        child.ChildProjections.Returns([grandchild]);
        _projection.ChildProjections.Returns([child]);

        _future = new ProjectionFuture(
            ProjectionFutureId.New(),
            _projection.Identifier,
            _event,
            new PropertyPath("children.grandchildren"),
            new PropertyPath("children.grandchildren.items"),
            new PropertyPath("itemId"),
            new PropertyPath("grandchildId"),
            new Key("grandchild-2", ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);
        _projectionFutures.GetFutures().Returns(_ => Task.FromResult<IEnumerable<ProjectionFuture>>(_resolved ? [] : [_future]));
        _projectionFutures.ResolveFuture(_future.Id).Returns(_ =>
        {
            _resolved = true;
            return Task.CompletedTask;
        });
        _tracker.HasPending = true;
    }

    protected async Task PerformWithState(ExpandoObject root)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.CurrentState.Returns(root);
        await _step.Perform(_projection, _context with { Changeset = changeset });
    }

    protected static ExpandoObject Child(string childId, params string[] grandchildIds)
    {
        var child = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)child;
        dictionary["childId"] = childId;
        dictionary["grandchildren"] = grandchildIds.Select(Grandchild).ToList();
        return child;
    }

    protected static ExpandoObject Root(params ExpandoObject[] children)
    {
        var root = new ExpandoObject();
        ((IDictionary<string, object?>)root)["children"] = children.ToList();
        return root;
    }

    static ExpandoObject Grandchild(string grandchildId)
    {
        var grandchild = new ExpandoObject();
        ((IDictionary<string, object?>)grandchild)["grandchildId"] = grandchildId;
        return grandchild;
    }
}
