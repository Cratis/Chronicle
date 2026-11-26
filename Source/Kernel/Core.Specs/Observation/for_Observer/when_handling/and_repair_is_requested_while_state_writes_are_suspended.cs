// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_repair_is_requested_while_state_writes_are_suspended : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource _subscriberStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _subscriberResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly given.reloadable_observer_state_storage _durableStorage = new();
    EventSequenceNumber _durableCursorAtRepairStart;
    EventSequenceNumber _durableCursorWhileSuspended;
    bool _repairStartedWhileSuspended;
    bool _acceptanceCompletedWhileSuspended;
    ObserverState _durableStateAtRepairStart;

    public and_repair_is_requested_while_state_writes_are_suspended()
    {
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _durableStorage : null!;
    }

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        _jobsManager.When(_ => _.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()))
            .Do(_ =>
            {
                _durableStateAtRepairStart = _durableStorage.PersistedState;
                _durableCursorAtRepairStart = _durableStateAtRepairStart.NextEventSequenceNumber;
            });
        _subscriber.OnNext((Key)"P", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ =>
            {
                _subscriberStarted.TrySetResult();
                return _subscriberResult.Task;
            });
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
    }

    async Task Because()
    {
        var delivery = _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 12UL)]);
        await _subscriberStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var acceptance = Task.CompletedTask;
        try
        {
            await _observer.RegisterCatchingUpPartitions(new HashSet<Key> { "B" });
            await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
            acceptance = _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
            _acceptanceCompletedWhileSuspended = acceptance.IsCompleted;
            _repairStartedWhileSuspended = _jobsManager.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Start");
            _durableCursorWhileSuspended = _durableStorage.PersistedState.NextEventSequenceNumber;
        }
        finally
        {
            _subscriberResult.TrySetResult(ObserverSubscriberResult.Ok(12UL));
            await Task.WhenAll(delivery, acceptance).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        await _silo.RunCatchUpContinuation().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_wait_for_the_suspended_write_in_the_original_acknowledgement() => _acceptanceCompletedWhileSuspended.ShouldBeFalse();
    [Fact] void should_not_start_repair_until_writes_resume() => _repairStartedWhileSuspended.ShouldBeFalse();
    [Fact] void should_keep_the_durable_cursor_while_suspended() => _durableCursorWhileSuspended.ShouldEqual((EventSequenceNumber)9UL);
    [Fact] void should_persist_the_accepted_cursor_before_starting_repair() => _durableCursorAtRepairStart.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_persist_the_missed_range_before_starting_repair() => _durableStateAtRepairStart.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("B", 10UL, 13UL));
    [Fact] void should_start_the_deferred_repair() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == (Key)"B" && _.FromSequenceNumber == (EventSequenceNumber)10UL));
}
