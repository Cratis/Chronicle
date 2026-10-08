// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_fetching_futures;

public class and_a_later_pass_fails_after_resolving_a_future : given.a_first_level_child_future
{
    Exception? _exception;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("ChildAdded", 1), 10);
        SetFutureKey("root-key");
        var rootEvent = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("RootCreated", 1), 5);
        _context = new ProjectionEventContext(
            new Key("root-key", ArrayIndexers.NoIndexers),
            rootEvent,
            new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), rootEvent, RootWith()),
            ProjectionOperationType.None,
            NeedsInitialState: false);
        _projectionFutures.GetFutures().Returns(
            Task.FromResult<IEnumerable<ProjectionFuture>>([_future]),
            Task.FromResult<IEnumerable<ProjectionFuture>>([_future]),
            Task.FromException<IEnumerable<ProjectionFuture>>(new InvalidOperationException()));
    }

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _step.Perform(_projection, _context));

    [Fact] void should_not_fail_the_event() => _exception.ShouldBeNull();
    [Fact] void should_resolve_the_future_once() => _projectionFutures.Received(1).ResolveFuture(_future.Id);
    [Fact] void should_preserve_the_resolved_event_position() => _result!.Event.ShouldEqual(_future.Event);
    [Fact] void should_preserve_the_pending_child_save() => HasChild.ShouldBeTrue();
    [Fact] void should_leave_futures_pending_for_retry() => _tracker.HasPending.ShouldBeTrue();
}
