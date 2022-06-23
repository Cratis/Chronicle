// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventSequences;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Partial Observer implementation focused on the reminder for failed partitions aspect.
/// </summary>
public partial class Observer
{
    /// <inheritdoc/>
    public async Task TryResumePartition(EventSourceId eventSourceId)
    {
        if (!HasSubscribedObserver || !State.IsPartitionFailed(eventSourceId))
        {
            return;
        }

        var failedPartition = State.GetFailedPartition(eventSourceId);
        if (State.IsRecoveringPartition(failedPartition.EventSourceId) &&
            _streamSubscriptionsByEventSourceId.ContainsKey(eventSourceId))
        {
            return;
        }

        State.StartRecoveringPartition(eventSourceId);
        await WriteStateAsync();

        if (_streamSubscriptionsByEventSourceId.ContainsKey(eventSourceId))
        {
            await _streamSubscriptionsByEventSourceId[eventSourceId]!.UnsubscribeAsync();
        }

        var tailSequenceNumber = await EventSequenceStorageProvider.GetTailSequenceNumber(State.EventSequenceId, State.EventTypes, eventSourceId);

        _logger.SubscribingToStream(_observerId, _eventSequenceId, _microserviceId, _tenantId, _stream!.Guid, _stream!.Namespace);

        _streamSubscriptionsByEventSourceId[eventSourceId] = await _stream!.SubscribeAsync(
            async (@event, _) => await HandleEventForRecoveringPartitionedObserver(@event, tailSequenceNumber),
            new EventSequenceNumberTokenWithFilter(failedPartition.SequenceNumber, State.EventTypes, eventSourceId));
    }

    async Task HandleEventForRecoveringPartitionedObserver(AppendedEvent @event, EventSequenceNumber tailSequenceNumber)
    {
        await HandleEventForPartitionedObserver(@event);
        if (State.IsPartitionFailed(@event.Context.EventSourceId))
        {
            await _streamSubscriptionsByEventSourceId[@event.Context.EventSourceId]!.UnsubscribeAsync();
            _streamSubscriptionsByEventSourceId.Remove(@event.Context.EventSourceId);
        }
        else
        {
            var partitionRecovery = State.GetPartitionRecovery(@event.Context.EventSourceId);
            partitionRecovery.SequenceNumber++;
            await WriteStateAsync();

            if (partitionRecovery.SequenceNumber >= tailSequenceNumber)
            {
                var actualTailSequenceNumber = await EventSequenceStorageProvider.GetTailSequenceNumber(State.EventSequenceId, State.EventTypes, @event.Context.EventSourceId);
                if (actualTailSequenceNumber == tailSequenceNumber)
                {
                    State.PartitionRecovered(@event.Context.EventSourceId);
                    await WriteStateAsync();

                    await _streamSubscriptionsByEventSourceId[@event.Context.EventSourceId]!.UnsubscribeAsync();
                    _streamSubscriptionsByEventSourceId.Remove(@event.Context.EventSourceId);
                }
            }
        }
    }

    async Task TryResumeAnyFailedPartitions()
    {
        if (State.HasFailedPartitions)
        {
            foreach (var partition in State.FailedPartitions)
            {
                await TryResumePartition(partition.EventSourceId);
            }
        }
    }
}
