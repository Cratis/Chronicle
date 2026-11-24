// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

/// <summary>
/// Right after a node loss the call that hands an event to the queues can be addressed to the dead silo and not answer
/// until the response timeout. The event is durable by then, so the append must be acknowledged without waiting for it;
/// a client that is told nothing for the whole timeout is told the outcome is unknown although the event is stored.
/// </summary>
public class and_the_live_dispatch_never_answers : given.an_event_sequence
{
    TaskCompletionSource _enqueue = null!;
    Task<AppendResult> _append = null!;
    bool _acknowledged;
    AppendResult _result;

    void Establish()
    {
        _enqueue = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _appendedEventsQueues
            .Enqueue(Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_ => _enqueue.Task);
    }

    async Task Because()
    {
        _append = _eventSequence.Append(
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
        _acknowledged = await Acknowledged(_append);
        if (_acknowledged) _result = await _append;
    }

    static async Task<bool> Acknowledged(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(3), TimeProvider.System);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    void Destroy() => _enqueue.TrySetResult();

    [Fact] void should_acknowledge_the_append_without_waiting_for_live_delivery() => _acknowledged.ShouldBeTrue();
    [Fact] void should_report_the_durable_append_as_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_handed_the_event_to_the_queues() => _appendedEventsQueues.Received(1).Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
}
