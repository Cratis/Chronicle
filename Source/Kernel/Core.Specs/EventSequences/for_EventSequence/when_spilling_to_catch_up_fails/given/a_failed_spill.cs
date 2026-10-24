// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_spilling_to_catch_up_fails.given;

/// <summary>
/// An event sequence whose live dispatch fails and whose spill to catch-up fails on its first attempt, the way both
/// calls fail while the grain directory still points at a silo that was just lost.
/// </summary>
public class a_failed_spill : for_EventSequence.given.an_event_sequence
{
    protected int _spillAttempts;
    protected int _timersBeforeAppend;

    protected virtual int FailingSpillAttempts => 1;

    void Establish()
    {
        _appendedEventsQueues
            .Enqueue(Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_ => Task.FromException(new for_EventSequence.given.SimulatedEnqueueError()));
        _appendedEventsQueues
            .SpillToCatchup()
            .Returns(_ => ++_spillAttempts <= FailingSpillAttempts
                ? Task.FromException(new for_EventSequence.given.SimulatedEnqueueError())
                : Task.CompletedTask);
        _timersBeforeAppend = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    protected Task AppendEvent() => _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);
}
