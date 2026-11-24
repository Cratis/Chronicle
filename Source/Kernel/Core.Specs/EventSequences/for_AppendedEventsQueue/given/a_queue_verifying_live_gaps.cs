// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class a_queue_verifying_live_gaps : a_queue_with_a_stored_durable_subscription
{
    protected IEventSequenceStorage _events;
    protected AppendedEvent[] _history = [];
    protected virtual bool StartsDormant => false;
    protected virtual ObserverFilters? LiveFilters => new(["included"]);

    protected static AppendedEvent MatchingAt(ulong sequence, string partition = "partition")
    {
        var @event = EventAt(sequence, partition);
        return @event with { Context = @event.Context with { Tags = [new Tag("included")] } };
    }

    protected async Task Deliver(params AppendedEvent[] events)
    {
        await _queue.Enqueue(events);
        await _queue.AwaitQueueDepletion();
    }

    async Task Establish()
    {
        _events = _storage.GetEventStore(EventStore).GetNamespace(Namespace).GetEventSequence(_eventSequence);
        _events.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var cursor = Substitute.For<IEventCursor>();
                cursor.Current.Returns(_history.Where(_ => _.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0)).ToArray());
                cursor.MoveNext().Returns(true, false);
                return cursor;
            });
        _observer.GetState().Returns(new ObserverState { NextEventSequenceNumber = 102UL, RunningState = ObserverRunningState.Active });
        await _queue.Subscribe(_observerKey, [_eventType], LiveFilters);
        if (StartsDormant) (await _queue.TryMarkDormant(_observerKey)).ShouldBeTrue();
    }
}
