// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// The step handles event 8 and then reads event 9, a redaction of an event type the observer does not subscribe to,
/// which it filters out rather than delivers. The observer looks for unread events of the types the step reads - the
/// redaction among them - so telling it the step got to 9, the event after the last one handled, would have it find
/// that redaction forever. The step tells it how far it read instead: 10 (#4583).
/// </summary>
public class and_its_last_event_is_one_it_reads_but_does_not_deliver : given.a_performing_job_step
{
    static readonly EventType _subscribedEventType = new("4f1d55f6-1c76-4c55-9a1f-55dce67e9a09", EventTypeGeneration.First);
    static readonly EventType _redactionEventType = new(GlobalEventTypes.Redaction, EventTypeGeneration.First);
    static readonly EventSequenceNumber _lastHandled = 8UL;
    static readonly EventSequenceNumber _lastRead = 9UL;

    readonly List<EventSequenceNumber> _delivered = [];
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.ConcludesPartitionCatchUp = true;
        _performState.StartEventSequenceNumber = _lastHandled;
        _performState.EventTypes = [_subscribedEventType, _redactionEventType];

        var redactionContent = new ExpandoObject();
        ((IDictionary<string, object?>)redactionContent)["originalEventType"] = "an-event-type-the-observer-does-not-subscribe-to";
        var redaction = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_redactionEventType, _lastRead) with { Content = redactionContent };

        var eventSequenceStorage = _storage
            .GetEventStore("event-store")
            .GetNamespace("event-store-namespace")
            .GetEventSequence(EventSequenceId.Log);
        eventSequenceStorage.GetRange(
            _lastHandled,
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(CursorWith(AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_subscribedEventType, _lastHandled), redaction)));
        eventSequenceStorage.GetRange(
            _lastRead,
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(CursorWith(redaction)));

        // The redaction is the only event at or after 9 - and nothing is left once it has been read.
        _observer.ConcludePartitionCatchUp(Arg.Any<Key>(), _lastRead, Arg.Any<IEnumerable<EventType>>()).Returns(false);
        _observer.ConcludePartitionCatchUp(Arg.Any<Key>(), _lastRead.Next(), Arg.Any<IEnumerable<EventType>>()).Returns(true);

        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _delivered.AddRange(events.Select(_ => _.Context.SequenceNumber));
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_deliver_only_the_event_it_does_not_filter_out() => _delivered.ShouldContainOnly(_lastHandled);
    [Fact] void should_tell_the_observer_it_read_past_the_filtered_event() => _observer.Received(1).ConcludePartitionCatchUp((Key)"some-partition", _lastRead.Next(), Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_not_tell_the_observer_it_only_got_as_far_as_the_last_event_handled() => _observer.DidNotReceive().ConcludePartitionCatchUp(Arg.Any<Key>(), _lastRead, Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_succeed() => (_result.TryGetResult(out var stepResult) && !stepResult.TryGetError(out _)).ShouldBeTrue();

    static IEventCursor CursorWith(params AppendedEvent[] events)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(events);
        var moved = false;
        cursor.MoveNext().Returns(_ =>
        {
            var hasBatch = !moved;
            moved = true;
            return Task.FromResult(hasBatch);
        });
        return cursor;
    }
}
