// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.given;

public class a_first_level_child_future : a_resolve_futures_step
{
    protected ProjectionFuture _future;
    protected bool _resolved;
    protected ProjectionEventContext? _result;
    protected object _rootKey = "root-key";

    void Establish()
    {
        var child = Substitute.For<IProjection>();
        child.Parent.Returns(_projection);
        child.Path.Returns(new ProjectionPath("children"));
        child.ChildrenPropertyPath.Returns(new PropertyPath("children"));
        child.OnNext(Arg.Do<ProjectionEventContext>(context =>
            context.Changeset.AddChild<ExpandoObject>(new PropertyPath("children"), new PropertyPath("childId"), "child-key", [], context.Key.ArrayIndexers)));
        _projection.ChildProjections.Returns([child]);
        _projectionFutures.GetFutures().Returns(_ => Task.FromResult<IEnumerable<ProjectionFuture>>(_resolved ? [] : [_future]));
        _tracker.HasPending = true;
    }

    protected void SetFutureKey(object key)
    {
        _rootKey = key;
        _future = new ProjectionFuture(
            ProjectionFutureId.New(),
            _projection.Identifier,
            _event,
            PropertyPath.Root,
            new PropertyPath("children"),
            new PropertyPath("childId"),
            new PropertyPath("childId"),
            new Key(key, ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);
        _projectionFutures.ResolveFuture(_future.Id).Returns(_ =>
        {
            _resolved = true;
            return Task.CompletedTask;
        });
    }

    protected async Task<ProjectionEventContext> ProcessRoot(ExpandoObject rootState, object? key = null, bool initializeNow = false)
    {
        var rootEvent = AppendedEvent.EmptyWithEventType(new EventType("RootCreated", EventTypeGeneration.First));
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), rootEvent, rootState);
        if (initializeNow)
        {
            changeset.SetInitialized(true);
        }

        _result = await _step.Perform(_projection, new ProjectionEventContext(
            new Key(key ?? _rootKey, ArrayIndexers.NoIndexers),
            rootEvent,
            changeset,
            ProjectionOperationType.None,
            NeedsInitialState: false));
        return _result;
    }

    protected static ExpandoObject RootWith(string? keyProperty = null, object? key = null, bool? initialized = true)
    {
        var root = new ExpandoObject();
        var dict = (IDictionary<string, object?>)root;
        if (keyProperty is not null) dict[keyProperty] = key;
        if (initialized is not null) dict[WellKnownProperties.ReadModelInstanceInitialized] = initialized.Value;
        return root;
    }

    protected bool HasChild => _result?.PendingFutureSaves.LastOrDefault() is { } save &&
        ((IDictionary<string, object?>)save.Changeset.CurrentState).TryGetValue("children", out var children) &&
        children is IEnumerable<object> list &&
        list.OfType<ExpandoObject>().Any(child => ((IDictionary<string, object?>)child)["childId"]?.ToString() == "child-key");
}
