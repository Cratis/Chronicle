// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Cratis.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_joining_a_real_queue : an_observer_with_a_handover_log
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(5);
    readonly TaskCompletionSource _spilled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AppendedEventsQueue _queue;
    int _catchUpRequests;

    protected virtual bool SpillDuringJoin => false;
    protected bool ClosedDuringJoin { get; private set; }
    protected bool LaneWasReplaced { get; private set; }
    protected bool WatchdogStartedAnotherJob { get; private set; }

    void Establish()
    {
        var tasks = Substitute.For<ITaskFactory>();
        tasks.Run(Arg.Any<Func<Task>>()).Returns(call => Task.Run(call.Arg<Func<Task>>()));
        var grains = Substitute.For<IGrainFactory>();
        var entry = Substitute.For<IObserver>();
        entry.GetState().Returns(_ => _observer.GetState());
        entry.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call => _observer.Handle(call.Arg<Key>(), call.Arg<IEnumerable<AppendedEvent>>()));
        entry.HandlePage(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>()).Returns(call => _observer.HandlePage(call.Arg<IReadOnlyList<ObserverPartitionBatch>>()));
        entry.CatchUp().Returns(_ =>
        {
            Interlocked.Increment(ref _catchUpRequests);
            _spilled.TrySetResult();
            return Task.CompletedTask;
        });
        grains.GetGrain<IObserver>(_observerKey).Returns(entry);
        var options = new ChronicleOptions { Events = new() { QueueBoundedEvents = 2 } };
        _queue = new AppendedEventsQueue(tasks, grains, Substitute.For<IMeter<AppendedEventsQueue>>(), Options.Create(options), NullLogger<AppendedEventsQueue>.Instance, storage: _storage);
        _queue.Identify(new EventSequenceKey(_observerKey.EventSequenceId, _observerKey.EventStore, _observerKey.Namespace), 0);
        _appendedEventsQueues.Subscribe(_observerKey, Arg.Any<EventTypeSet>(), Arg.Any<ObserverFilters?>()).Returns(async call =>
        {
            await _queue.Subscribe(_observerKey, call.Arg<EventTypeSet>(), call.Arg<ObserverFilters?>());
            return new AppendedEventsQueueSubscription(_observerKey, 0);
        });
        _appendedEventsQueues.IsSubscribed(_observerKey).Returns(async _ => (await _queue.GetSubscriptions()).Count == 1);
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(_ => _queue.Unsubscribe(_observerKey));
    }

    protected async Task JoinAndProbe()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 11UL };
        Append("P", 11UL);
        await _observer.PartitionFailed("P", 11UL, ["permanently failed"], string.Empty);
        Append("Q", 12UL);
        Append("R", 13UL);
        await _observer.CaughtUp(JobId.NotSet, 10UL, 10UL);
        if (SpillDuringJoin) _writes.AfterWrite = SpillAfterCheckpoint;
        await _silo.RunCatchUpContinuation().WaitAsync(_deadline, TimeProvider.System);
        if (SpillDuringJoin)
        {
            // TestKit has no Orleans dispatcher. Run the real queue's ordinary CatchUp request on the next turn.
            await _observer.CatchUp();
            await _silo.RunCatchUpContinuation().WaitAsync(_deadline, TimeProvider.System);
        }
        LaneWasReplaced = (await _queue.GetSubscriptions()).Count == 1;
        await _queue.Enqueue([Append("Q", SpillDuringJoin ? 17UL : 14UL)]);
        await _queue.AwaitQueueDepletion(500, 10);
        var wide = _wide;
        await _observer.RunWatchdogOnce();
        WatchdogStartedAnotherJob = !ReferenceEquals(wide, _wide) || Volatile.Read(ref _catchUpRequests) != (SpillDuringJoin ? 1 : 0);
    }

    async Task SpillAfterCheckpoint(ObserverState written)
    {
        if (written.NextEventSequenceNumber <= 12UL) return;
        _writes.AfterWrite = _ => Task.CompletedTask;
        await _queue.Enqueue([Append("Q", 14UL), Append("R", 15UL), Append("Q", 16UL)]);
        await _spilled.Task.WaitAsync(_deadline, TimeProvider.System);
        ClosedDuringJoin = (await _queue.GetSubscriptions()).Count == 0;
    }
}
