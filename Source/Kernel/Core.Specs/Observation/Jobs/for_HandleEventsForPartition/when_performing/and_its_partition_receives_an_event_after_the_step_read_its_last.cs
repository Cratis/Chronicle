// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// Partition B's step reads to event 9 and runs dry while partition A's step goes on to 12. Event 11 for B is appended
/// after B's cursor ran dry, so live delivery dropped it while B was still catching up. The observer's position later
/// moves to 13, past it. The step must read on and handle the event itself instead of leaving it to nobody (#4583).
/// </summary>
public class and_its_partition_receives_an_event_after_the_step_read_its_last : given.a_performing_job_step
{
    static readonly EventSequenceNumber _lastReadBeforeRunningDry = 9UL;
    static readonly EventSequenceNumber _appendedAfterRunningDry = 11UL;

    readonly List<EventSequenceNumber> _delivered = [];
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.ConcludesPartitionCatchUp = true;
        _performState.StartEventSequenceNumber = 5UL;

        var eventSequenceStorage = _storage
            .GetEventStore("event-store")
            .GetNamespace("event-store-namespace")
            .GetEventSequence(EventSequenceId.Log);
        eventSequenceStorage.GetRange(
            _performState.StartEventSequenceNumber,
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(CursorWith(_lastReadBeforeRunningDry)));
        eventSequenceStorage.GetRange(
            _lastReadBeforeRunningDry.Next(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(CursorWith(_appendedAfterRunningDry)));

        // The observer finds the event dropped while B was catching up, then nothing once the step has read it.
        _observer.ConcludePartitionCatchUp(Arg.Any<Key>(), _lastReadBeforeRunningDry.Next(), Arg.Any<IEnumerable<EventType>>()).Returns(false);
        _observer.ConcludePartitionCatchUp(Arg.Any<Key>(), _appendedAfterRunningDry.Next(), Arg.Any<IEnumerable<EventType>>()).Returns(true);

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

    [Fact] void should_handle_the_event_appended_after_running_dry_exactly_once() => _delivered.Count(_ => _ == _appendedAfterRunningDry).ShouldEqual(1);
    [Fact] void should_handle_every_event_once_in_order() => _delivered.ShouldContainOnly(_lastReadBeforeRunningDry, _appendedAfterRunningDry);
    [Fact] void should_succeed() => (_result.TryGetResult(out var stepResult) && !stepResult.TryGetError(out _)).ShouldBeTrue();
    [Fact] void should_conclude_the_partition_once_nothing_is_left_unread() => _observer.Received(1).ConcludePartitionCatchUp((Key)"some-partition", _appendedAfterRunningDry.Next(), Arg.Any<IEnumerable<EventType>>());

    static IEventCursor CursorWith(EventSequenceNumber sequenceNumber)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns([AppendedEvent.EmptyWithEventSequenceNumber(sequenceNumber)]);
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
