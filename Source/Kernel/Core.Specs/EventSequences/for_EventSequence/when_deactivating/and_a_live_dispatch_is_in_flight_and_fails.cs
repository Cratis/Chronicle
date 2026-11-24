// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_deactivating;

/// <summary>
/// An acknowledged event whose hand-over to the queues is still in flight when the grain deactivates, as when a silo is
/// stopped. The hand-over then fails, and nothing outside this activation knows the event was never handed over: unless
/// the deactivation spills, an observer handed a later event moves past it and it is never delivered.
/// </summary>
public class and_a_live_dispatch_is_in_flight_and_fails : given.an_event_sequence
{
    TaskCompletionSource _enqueue = null!;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _enqueue = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _appendedEventsQueues.Enqueue(Arg.Any<IEnumerable<AppendedEvent>>()).Returns(_ => _enqueue.Task);
        await AppendAnEvent();
    }

    async Task Because()
    {
        var deactivation = _eventSequence.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
        _enqueue.SetException(new given.SimulatedEnqueueError());
        await deactivation;
    }

    [Fact] void should_spill_to_catch_up_before_going_away() => _appendedEventsQueues.Received().SpillToCatchup();
}
