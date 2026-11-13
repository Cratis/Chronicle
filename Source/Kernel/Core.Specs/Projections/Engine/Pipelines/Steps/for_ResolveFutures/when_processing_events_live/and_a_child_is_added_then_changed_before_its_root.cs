// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_child_is_added_then_changed_before_its_root : given.two_deferred_events_for_one_child
{
    ProjectionEventContext _rootContext;
    PendingFutureSave[] _pending;
    IDictionary<string, object?>[] _children;
    int _deferredBeforeRoot;
    int _unacknowledgedBeforeSave;
    int _remaining;

    async Task Because()
    {
        await Process(Event(_childAdded, 1, new { id = "child", name = "Child", email = "initial@example.com" }));
        await Process(Event(_emailChanged, 2, new { id = "child", email = "updated@example.com" }));
        _deferredBeforeRoot = (await _futures.GetFutures()).Count();
        _rootContext = await Process(Event(_rootCreated, 3, new { name = "Root" }), save: false);
        _pending = _rootContext.PendingFutureSaves.ToArray();
        _unacknowledgedBeforeSave = (await _futures.GetFutures()).Count();
        await _save.Perform(_projection, _rootContext);
        _remaining = (await _futures.GetFutures()).Count();
        var stored = (IDictionary<string, object?>)(await _sink.FindOrDefault(_rootContext.Key))!;
        _children = ((IEnumerable<object>)stored["children"]!).Cast<IDictionary<string, object?>>().ToArray();
    }

    async Task<ProjectionEventContext> Process(AppendedEvent incoming, bool save = true)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, new ExpandoObject());
        var context = new ProjectionEventContext(Key.Undefined, incoming, changeset, _projection.GetOperationTypeFor(incoming.Context.EventType), false);
        foreach (var step in _beforeResolve)
        {
            context = await step.Perform(_projection, context);
        }
        context = await _resolve.Perform(_projection, context);
        if (save)
        {
            context = await _save.Perform(_projection, context);
        }
        return context;
    }

    [Fact] void should_defer_both_events_until_the_root_arrives() => _deferredBeforeRoot.ShouldEqual(2);
    [Fact] void should_resolve_both_in_the_root_turn() => _pending.Length.ShouldEqual(2);
    [Fact] void should_not_acknowledge_before_saving() => _unacknowledgedBeforeSave.ShouldEqual(2);
    [Fact] void should_keep_one_child_in_the_sink() => _children.Length.ShouldEqual(1);
    [Fact] void should_keep_the_child_identifier() => _children[0]["id"].ShouldEqual("child");
    [Fact] void should_preserve_the_unrelated_creation_field() => _children[0]["name"].ShouldEqual("Child");
    [Fact] void should_store_the_updated_email() => _children[0]["email"].ShouldEqual("updated@example.com");
    [Fact] void should_not_add_the_child_again_for_the_update() => _pending[1].Changeset.Changes.OfType<ChildAdded>().ShouldBeEmpty();
    [Fact] void should_update_the_email_in_the_second_changeset() => _pending[1].Changeset.Changes
        .OfType<PropertiesChanged<ExpandoObject>>()
        .SelectMany(change => change.Differences)
        .Single(difference => difference.PropertyPath.LastSegment.Value == "email")
        .Changed.ShouldEqual("updated@example.com");
    [Fact] void should_acknowledge_both_after_saving() => _remaining.ShouldEqual(0);
}
