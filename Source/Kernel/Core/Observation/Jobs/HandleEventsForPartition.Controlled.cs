// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs;

public partial class HandleEventsForPartition
{
    Exception? _controlledRunException;
    bool _controlledRunClosed;
    EventSequenceNumber _controlledCountedThrough = EventSequenceNumber.Unavailable;

    async Task<Catch<(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents), None>> HandleControlledRuns(
        HandleEventsForPartitionState state,
        AppendedEvent[] filtered,
        TimeSpan timeout,
        ObserverSubscriberContext context,
        CancellationToken cancellationToken)
    {
        var eligible = ObserverDeliveryRuns.Eligible(_performSubscription, filtered);
        var remaining = eligible;
        var lastSuccessful = EventSequenceNumber.Unavailable;
        while (remaining.Length > 0)
        {
            var target = ObserverDeliveryRuns.Select(_performSubscription, state.Partition, remaining[0], subscriberSelector);
            if (target is null)
            {
                _pendingHold = ControlledRecovery.HoldAt(remaining[0], state);
                break;
            }

            var run = ObserverDeliveryRuns.Take(target, remaining);
            BindToTarget(state.Partition, target);
            var result = await DeliverControlledRun(state, target, run, timeout, context);
            if (result.State == ObserverSubscriberState.Disconnected)
            {
                _performSubscription = _performSubscription with
                {
                    Targets = _performSubscription.Targets.Where(existing => existing != target).ToArray()
                };
                continue;
            }

            if (result.State != ObserverSubscriberState.Ok)
            {
                return (result with
                {
                    LastSuccessfulObservation = result.LastSuccessfulObservation.IsActualValue ? result.LastSuccessfulObservation : lastSuccessful
                }, eligible);
            }

            lastSuccessful = run[^1].Context.SequenceNumber;
            if (!await ReportFromWork(lastSuccessful))
            {
                _controlledRunClosed = true;
                return default(None);
            }
            await ReportHandledEvents(state.Partition, run, lastSuccessful, cancellationToken);
            remaining = remaining.Skip(run.Length).ToArray();
        }

        return lastSuccessful.IsActualValue ? (ObserverSubscriberResult.Ok(lastSuccessful), eligible) : default(None);
    }

    PartitionFailureContext? ControlledFailureContext(HandleEventsForPartitionState state, EventSequenceNumber failedAt) =>
        _performSubscription.TypeSetMembers is not null ? ControlledRecovery.ContextFor(state, failedAt) : null;

    async Task<ObserverSubscriberResult> DeliverControlledRun(
        HandleEventsForPartitionState state,
        ObserverSubscriberTarget target,
        AppendedEvent[] run,
        TimeSpan timeout,
        ObserverSubscriberContext context)
    {
        try
        {
            var released = await DecryptEvents(run, target);
            var selectedContext = context with { Metadata = target.ConnectedClient ?? _performSubscription.Arguments };
            _deliveryFence = AbandonedDeliveryDrain.Capture(_performSubscription, state.Partition, target, selectedContext);
            return await _subscriber!.OnNextWithin(timeout, state.Partition, released, selectedContext);
        }
        catch (Exception exception)
        {
            _controlledRunException = exception;
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, exception.Message) with
            {
                FailedAt = run[0].Context.SequenceNumber,
                ExceptionStackTrace = exception.StackTrace ?? string.Empty
            };
        }
    }
}
