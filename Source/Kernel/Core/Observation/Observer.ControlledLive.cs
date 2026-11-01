// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    static EventSequenceNumber ControlledFailurePosition(ObserverSubscriberResult result, AppendedEvent[] run)
    {
        if (result.FailedAt.IsActualValue) return result.FailedAt;
        if (!result.LastSuccessfulObservation.IsActualValue) return run[0].Context.SequenceNumber;
        return run.FirstOrDefault(@event => @event.Context.SequenceNumber > result.LastSuccessfulObservation)?.Context.SequenceNumber ?? run[0].Context.SequenceNumber;
    }

    async Task HandleControlledLiveRuns(Key partition, IEnumerable<AppendedEvent> events, Activity? activity)
    {
        if (!ShouldHandleEvent(partition)) return;
        var filters = _subscription.Filters;
        var filtered = events.Where(@event => @event.Context.SequenceNumber >= State.NextEventSequenceNumber && filters?.Matches(@event) != false);
        var remaining = ObserverDeliveryRuns.Eligible(_subscription, filtered);
        while (remaining.Length > 0)
        {
            var first = remaining[0];
            var selected = ObserverDeliveryRuns.Select(_subscription, partition, first, subscriberSelector);
            if (selected is null)
            {
                await PartitionHeld(partition, new(first.Context.SequenceNumber, FailedPartitionRecoveryMode.Live, null), first.Context.EventType.Id);
                return;
            }

            var run = ObserverDeliveryRuns.Take(selected, remaining);
            await HandleSelectedEventsForPartition(partition, run, activity, selected);
            if (Failures.IsFailed(partition) || !_subscription.IsSubscribed) return;
            remaining = remaining.Skip(run.Length).ToArray();
        }
    }
}
