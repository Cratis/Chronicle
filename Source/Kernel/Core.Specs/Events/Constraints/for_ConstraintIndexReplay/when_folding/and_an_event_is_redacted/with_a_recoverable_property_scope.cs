// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding.and_an_event_is_redacted;

public class with_a_recoverable_property_scope : given.a_replay
{
    ClosedStream _closure;

    void Establish()
    {
        _closing = _closing with { Dimensions = ClosedStreamDimensions.EventStreamId, EventStreamIdFrom = "stream" };
        Event(5, "source", GlobalEventTypes.Redaction.Value);
        ((IDictionary<string, object?>)_events[0].Content)["originalEventType"] = "Closed";
        _closure = new(new(EventStreamId: "stream"), "closing", 5, null);
        _closureStorage = Substitute.For<IClosedStreamsConstraintStorage>();
        _closureStorage.GetForEvent("closing", (EventSequenceNumber)5, Arg.Any<CancellationToken>()).Returns([_closure]);
        _closureStorage.GetFor("closing", _closure.Scope, Arg.Any<CancellationToken>()).Returns(_closure);
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_recover_without_an_error() => _result.SettledWithError.ShouldEqual(0);
    [Fact] void should_finish_the_fold() => _result.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_read_only_the_deciding_event() => await _closureStorage.Received(1).GetForEvent("closing", (EventSequenceNumber)5, Arg.Any<CancellationToken>());
    [Fact] async Task should_not_read_every_closure_for_the_owner() => await _closureStorage.DidNotReceive().GetForOwner(Arg.Any<ClosedStreamOwner>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_preserve_the_applied_closure() => await _closureStorage.DidNotReceive().Close(Arg.Any<ClosedStream>(), Arg.Any<CancellationToken>());
}
