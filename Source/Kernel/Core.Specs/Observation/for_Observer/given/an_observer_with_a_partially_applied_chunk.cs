// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_partially_applied_chunk : an_observer_with_subscription_for_specific_event_type
{
    const int AppliedPrefix = 137;
    protected readonly List<Key[]> _attempts = [];
    protected readonly Dictionary<Key, ulong> _persisted = [];
    protected readonly List<string> _freshPartitions = ["partition-0", "partition-512", "partition-700", "fresh"];
    protected Exception _error;
    protected ulong _totalAtFailure;
    protected ulong _runningTotalAtFailure;
    protected ulong _perTypeAtFailure;
    protected readonly TaskCompletionSource _writeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _releaseWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected virtual bool IsLive => false;
    protected virtual bool HasConfirmedPrefix => true;
    protected virtual ulong OriginalCount => 2UL;
    protected virtual ulong FreshCount => 5UL;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 600 };

    async Task Establish()
    {
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => ObserverSubscriberResult.Ok(call.Arg<IEnumerable<AppendedEvent>>().Last().Context.SequenceNumber));
        _observerHandledCountsStorage.Increment(_observerId, Arg.Any<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>())
            .Returns(call => Persist(call.Arg<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>()));
        for (var index = 0; index < 1100; index++)
        {
            await Deliver($"partition-{index}", (ulong)index, OriginalCount);
        }
    }

    protected async Task Deliver(string partition, ulong sequence, ulong count)
    {
        if (IsLive)
        {
            await _observer.Handle((Key)partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, sequence)]);
            return;
        }

        var step = JobStepId.New();
        await _observer.ReportHandledEventsBatch(step, [new HandledEventsReport(partition, new Dictionary<EventTypeId, EventCount> { [event_type.Id] = count }, step, sequence)]);
    }

    protected bool EveryPartitionHasItsExactCount() => Enumerable.Range(0, 1100).All(index =>
        _persisted[(Key)$"partition-{index}"] == OriginalCount + (_freshPartitions.Contains($"partition-{index}") ? FreshCount : 0UL));

    async Task Persist(IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>> counts)
    {
        _attempts.Add(counts.Select(_ => _.Key).ToArray());
        var applied = _attempts.Count == 2 ? counts.Take(AppliedPrefix) : counts;
        foreach (var (partition, perType) in applied)
        {
            _persisted[partition] = _persisted.GetValueOrDefault(partition) + perType[event_type.Id].Value;
        }

        if (_attempts.Count == 2)
        {
            _writeEntered.TrySetResult();
            await _releaseWrite.Task;
            var error = new InvalidOperationException("rejected");
            if (HasConfirmedPrefix)
            {
                throw new ObserverHandledCountsPartiallyIncremented(AppliedPrefix, error);
            }

            throw error;
        }
    }
}
