// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

public class and_a_conclusion_interleaves_before_page_settlement : given.an_observer_with_a_page_subscriber
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(5);
    readonly given.reloadable_observer_state_storage _writes = new();
    readonly TaskCompletionSource _submitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverPageResult> _outcomes = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _durableFailureWrites;
    AppendedEvent[] _log;

    /// <summary>
    /// Records the observer state accepted by storage separately from the live grain state.
    /// </summary>
    public and_a_conclusion_interleaves_before_page_settlement() =>
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _writes : null!;

    async Task Establish()
    {
        _log = [At("P", 11UL), At("A", 14UL), At("B", 15UL), At("A", 16UL)];
        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call => given.an_observer_with_a_handover_log.CreateCursor(_log.Where(@event => WithinScope(@event, call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSequenceNumber>(1), call.ArgAt<EventSourceId?>(2))).ToArray()));
        _observer.SetSubscription(subscription with { SubscriberType = typeof(IObserverSubscriber) });
        await _observer.RegisterCatchingUpPartitions([(Key)"P"]);
        await _observer.Handle("P", [_log[0]]);
        _observer.SetSubscription(subscription);
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ =>
            {
                _submitted.TrySetResult();
                return _outcomes.Task;
            });
    }

    async Task Because()
    {
        var page = _observer.HandlePage([new("A", [_log[1], _log[3]]), new("B", [_log[2]])]);
        await _submitted.Task.WaitAsync(_deadline, TimeProvider.System);
        var conclusion = _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        var secondPage = _observer.HandlePage([new("C", [At("C", 17UL)]), new("D", [At("D", 18UL)])]);
        var writesBefore = _failedPartitionsStorageStats.Writes;
        _outcomes.SetResult(new(new Dictionary<Key, ObserverSubscriberResult>
        {
            [(Key)"A"] = ObserverSubscriberResult.Ok(16UL),
            [(Key)"B"] = ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "B did not handle the event")
        }));
        await Task.WhenAll(page, conclusion, secondPage).WaitAsync(_deadline, TimeProvider.System);
        _durableFailureWrites = _failedPartitionsStorageStats.Writes - writesBefore;
    }

    [Fact] void should_have_submitted_only_the_original_page() => _pageSubscriber.Received(1).OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_resend_any_partition_of_the_page() => _pageSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_account_for_as_settled_progress() => _writes.PersistedState.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)16UL);
    [Fact] void should_record_only_b_as_failed() => _failedPartitionsState.Partitions.Select(_ => _.Partition).ShouldContainOnly((Key)"B");
    [Fact] void should_park_b_at_its_original_first_event() => _failedPartitionsState.Partitions.SelectMany(_ => _.Attempts).Select(_ => _.SequenceNumber).ShouldContainOnly((EventSequenceNumber)15UL);
    [Fact] void should_write_the_failure() => _durableFailureWrites.ShouldBeGreaterThan(0);
    [Fact] void should_clear_the_submitted_partitions_restart_markers() => _writes.PersistedState.InFlightPartitions.ShouldBeEmpty();

    static bool WithinScope(AppendedEvent @event, EventSequenceNumber from, EventSequenceNumber to, EventSourceId? source) =>
        @event.Context.SequenceNumber >= from && @event.Context.SequenceNumber <= to && (source is null || @event.Context.EventSourceId == source);

    AppendedEvent At(EventSourceId source, ulong number)
    {
        var @event = EventAt(number);
        return @event with { Context = @event.Context with { EventSourceId = source } };
    }
}
