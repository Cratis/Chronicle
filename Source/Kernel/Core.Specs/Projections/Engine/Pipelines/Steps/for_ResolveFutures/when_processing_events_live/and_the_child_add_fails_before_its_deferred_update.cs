// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_the_child_add_fails_before_its_deferred_update : given.two_deferred_events_for_one_child
{
    ProjectionEventContext _context;
    PendingFutureSave[] _pending;
    ProjectionFuture[] _remaining;
    FailedPartition _failure;
    int _deferredBeforeRoot;
    int _addAttempts;
    int _updateAttempts;
    int _storedChildCount;

    async Task Because()
    {
        await Process(Event(_childAdded, 1, new { id = "child", name = "Child", email = "initial@example.com" }));
        await Process(Event(_emailChanged, 2, new { id = "child", email = "updated@example.com" }));
        _deferredBeforeRoot = (await _futures.GetFutures()).Count();
        _context = await Process(Event(_rootCreated, 3, new { name = "Root" }));
        _pending = _context.PendingFutureSaves.ToArray();
        _failure = new FailedPartition(_pending[0].Key, _pending[0].Changeset.Incoming.Context.SequenceNumber);
        var writingSink = Substitute.For<ISink>();
        writingSink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>())
            .Returns(call => _sink.ApplyChanges(
                call.ArgAt<Key>(0),
                call.ArgAt<IChangeset<AppendedEvent, ExpandoObject>>(1),
                call.ArgAt<EventSequenceNumber>(2),
                call.ArgAt<SinkWriteMode>(3)));
        writingSink.ApplyChangesImmediately(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>())
            .Returns(call => ApplyPending(call.ArgAt<Key>(0), call.ArgAt<IChangeset<AppendedEvent, ExpandoObject>>(1)));
        var save = new SaveChanges(_futures, writingSink, Substitute.For<IChangesetStorage>(), false, NullLogger<SaveChanges>.Instance);
        await save.Perform(_projection, _context);
        _remaining = (await _futures.GetFutures()).ToArray();
        _storedChildCount = (await StoredChildren(_context.Key)).Count();
    }

    async Task<ProjectionEventContext> Process(AppendedEvent incoming)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, new ExpandoObject());
        var context = new ProjectionEventContext(Key.Undefined, incoming, changeset, _projection.GetOperationTypeFor(incoming.Context.EventType), false);
        foreach (var step in _beforeResolve)
        {
            context = await step.Perform(_projection, context);
        }
        return await _resolve.Perform(_projection, context);
    }

    async Task<IEnumerable<FailedPartition>> ApplyPending(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        if (changeset.Changes.OfType<ChildAdded>().Any())
        {
            _addAttempts++;
            return [_failure];
        }

        _updateAttempts++;
        var children = await StoredChildren(key);
        if (children.Any(child => Equals(child["id"], "child")))
        {
            throw new InvalidOperationException("The no-match field-update seam requires the child to be absent.");
        }

        // Model an acknowledged field update with no matching child: no mutation and no reported failure.
        return [];
    }

    async Task<IEnumerable<IDictionary<string, object?>>> StoredChildren(Key key)
    {
        var state = (IDictionary<string, object?>)(await _sink.FindOrDefault(key))!;
        return state.TryGetValue("children", out var children) && children is IEnumerable<object> items
            ? items.Cast<IDictionary<string, object?>>()
            : [];
    }

    [Fact] void should_defer_both_events_before_the_root() => _deferredBeforeRoot.ShouldEqual(2);
    [Fact] void should_queue_both_writes_in_the_root_turn() => _pending.Length.ShouldEqual(2);
    [Fact] void should_attempt_the_child_add_once() => _addAttempts.ShouldEqual(1);
    [Fact] void should_record_the_add_failure() => _context.FailedPartitions.ShouldContainOnly(_failure);
    [Fact] void should_queue_the_email_as_a_property_update() => _pending[1].Changeset.Changes
        .OfType<PropertiesChanged<ExpandoObject>>()
        .SelectMany(change => change.Differences)
        .Single(difference => difference.PropertyPath.LastSegment.Value == "email")
        .Changed.ShouldEqual("updated@example.com");
    [Fact] void should_leave_the_child_absent_after_the_add_fails() => _storedChildCount.ShouldEqual(0);
    [Fact] void should_keep_the_add_future_for_retry() => _remaining.Any(future => future.Id == _pending[0].FutureId).ShouldBeTrue();
    [Fact] void should_not_apply_the_dependent_update_after_the_add_fails() => _updateAttempts.ShouldEqual(0);
    [Fact] void should_keep_the_update_future_for_retry() => _remaining.Any(future => future.Id == _pending[1].FutureId).ShouldBeTrue();
    [Fact] void should_keep_both_futures_for_retry() => _remaining.Length.ShouldEqual(2);
}
