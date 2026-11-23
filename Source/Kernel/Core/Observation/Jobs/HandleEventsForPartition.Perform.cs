// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Monads;
using OneOf.Types;
using HandledBatch = Cratis.Monads.Catch<(Cratis.Chronicle.Observation.ObserverSubscriberResult Result, Cratis.Chronicle.Concepts.Events.AppendedEvent[] HandledEvents), OneOf.Types.None>;

namespace Cratis.Chronicle.Observation.Jobs;

public partial class HandleEventsForPartition
{
    static EventSequenceNumber GetLastScanned(EventSequenceNumber lastScanned, BoundedEventCursor events)
    {
        foreach (var @event in events.Current)
        {
            if (!lastScanned.IsActualValue || @event.Context.SequenceNumber > lastScanned)
            {
                lastScanned = @event.Context.SequenceNumber;
            }
        }

        return lastScanned;
    }

    static EventSequenceNumber NextFailedAt(HandleEventsForPartitionState currentState, EventSequenceNumber lastHandled) =>
        lastHandled.IsActualValue ? lastHandled.Next() : currentState.StartEventSequenceNumber;

    static BatchFailure FailureOf(Exception exception)
    {
        var failure = new BatchFailure(exception.GetAllMessages().ToArray(), exception.StackTrace ?? string.Empty, exception.ToFailureKind());
        return failure.Kind == FailureKind.Timeout
            ? failure with { Messages = [TransientTimeouts.ShortMessageFor(exception)], StackTrace = string.Empty }
            : failure;
    }

    async Task<Catch<JobStepResult>> PerformPreparedStep(HandleEventsForPartitionState currentState, CancellationToken cancellationToken)
    {
        var progress = new PerformProgress();
        var subscription = await _observer.GetSubscription();
        _generationDelivery = subscription.GenerationDelivery;

        // A step is prepared, and its grain activated, when its job starts, but a job keeps only a window of its steps
        // running, so a step can start long after. The client that was subscribed when it was prepared may be gone by
        // then, and delivering through its connection would find nobody. This runs on the step's background task, which
        // has no access to the grain's own services, hence the factory kept from when the step was prepared or activated.
        if (subscription.IsSubscribed)
        {
            RefreshSubscriber(subscription, currentState.Partition);
        }

        try
        {
            progress.LastHandled = currentState.LastSuccessfullyHandledEventSequenceNumber;
            if (_subscriber is null || !subscription.IsSubscribed)
            {
                logger.PerformingStoppedUnsubscribed(currentState.Partition);
                return JobStepResult.Failed(SubscriberDisconnected, "This should have been ensured in the Prepare operation");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                logger.CancelledBeforeHandlingAnyEvents(currentState.Partition);
                return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
            }

            var eventTypesToRead = await ResolveEventTypesToRead(currentState, subscription);
            using var events = await OpenEventCursor(currentState, subscription, eventTypesToRead, cancellationToken);
            var plan = await CreateDeliveryPlan(currentState, subscription, events, cancellationToken);
            return await DeliverBatches(plan, progress, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return CancelledResult(currentState.Partition, progress.LastHandled);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CancelledResult(currentState.Partition, progress.LastHandled);
        }
        catch (Exception e) when (TransientTimeouts.IsTimeout(e))
        {
            return TimedOutResult(currentState, progress.LastHandled, e);
        }
        catch (Exception e)
        {
            return await FailedUnexpectedlyResult(currentState, progress.LastHandled, e);
        }
    }

    void RefreshSubscriber(ObserverSubscription subscription, Key partition)
    {
        var subscriberKey = GetObserverSubscriberKey(subscription, partition);
        if (_subscriberKey is not null && _subscriberKey != subscriberKey)
        {
            _subscriber = _grainFactory.GetGrain(subscription.SubscriberType, subscriberKey) as IObserverSubscriber;
        }

        _subscriberKey = subscriberKey;
        _isCollapsingProjection = subscription.IsCollapsingProjection;
    }

    async Task<EventType[]> ResolveEventTypesToRead(HandleEventsForPartitionState currentState, ObserverSubscription subscription)
    {
        var scopedEventTypes = ObserverReaderScope
            .ForRequest(currentState.ReaderScope, currentState.EventTypes)
            .EventTypesToRead(currentState.EventTypes);
        var eventTypesToRead = scopedEventTypes?.ToArray()
            ?? await ResolveFallbackEventTypesToRead(subscription.EventTypes);
        _generationPins = eventTypesToRead.ToArray();
        _generationEventStore = currentState.ObserverKey.EventStore;
        _eventTypes = storage.GetEventStore(currentState.ObserverKey.EventStore).EventTypes;
        _eventTypeSchemas = (await _eventTypes.GetFor(eventTypesToRead))
            .ToDictionary(_ => _.Type);
        return eventTypesToRead;
    }

    /// <summary>
    /// Resolve the event types to read when the observer's subscription itself carries none - an observer
    /// subscribed to all events has no fixed list, since new event types can be registered after it subscribed,
    /// so its full, current set is resolved from the observer rather than trusted from the subscription snapshot.
    /// </summary>
    /// <param name="subscriptionEventTypes">The event types recorded on the current subscription.</param>
    /// <returns>The event types to read.</returns>
    async Task<EventType[]> ResolveFallbackEventTypesToRead(IEnumerable<EventType> subscriptionEventTypes)
    {
        var eventTypes = subscriptionEventTypes.ToArray();
        return eventTypes.Length != 0 ? eventTypes : (await _observer.GetEventTypes()).ToArray();
    }

    async Task<BoundedEventCursor> OpenEventCursor(
        HandleEventsForPartitionState currentState,
        ObserverSubscription subscription,
        EventType[] eventTypesToRead,
        CancellationToken cancellationToken)
    {
        // A storage batch can hold thousands of events; a client or webhook subscriber gets them a bounded call
        // at a time, so its timeout guards a known amount of work and every answered call is checkpointed.
        var maxEventsPerCall = await configurationProvider.GetMaxEventsPerSubscriberCallForObserver(currentState.ObserverKey, subscription.SubscriberType);
        var storageEvents = Storage.EventSequences.BoundedEventCursor.Over(
            await OpenDurableRange(currentState, subscription, eventTypesToRead, cancellationToken),
            maxEventsPerCall);
        var maximumEventsPerCall = _subscriber is IUnpartitionedObserverSubscriber
            ? Math.Min(MaxEventsPerBatch, ObserverSubscriberExtensions.MaximumEventsPerUnpartitionedCall)
            : MaxEventsPerBatch;
        return new BoundedEventCursor(storageEvents, maximumEventsPerCall);
    }

    async Task<DeliveryPlan> CreateDeliveryPlan(
        HandleEventsForPartitionState currentState,
        ObserverSubscription subscription,
        BoundedEventCursor events,
        CancellationToken cancellationToken)
    {
        // The pinned event types are exactly the ones that were read.
        var nonRedactionEventTypeIds = _generationPins
            .Where(et => et.Id != GlobalEventTypes.Redaction)
            .Select(et => et.Id)
            .ToHashSet();
        var subscriberContext = CreateSubscriberContext(subscription, currentState);
        var subscriberTimeout = await configurationProvider.GetSubscriberTimeoutForObserver(currentState.ObserverKey);
        await DrainAbandonedDeliveryBeforeHandling(subscription, currentState, cancellationToken);
        return new(currentState, events, subscriberContext, subscriberTimeout, nonRedactionEventTypeIds, subscription.Filters);
    }

    async Task<JobStepResult> DeliverBatches(DeliveryPlan plan, PerformProgress progress, CancellationToken cancellationToken)
    {
        while (await plan.Events.MoveNext())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogCancelled(progress.LastAttempted, plan.State.Partition);
                return ClosedResult(progress.LastHandled);
            }

            if (await IsClosedForResume(plan.State))
            {
                return ClosedResult(progress.LastHandled);
            }

            var handleEventsResult = await HandleBatch(plan, cancellationToken);
            if (_subscriber is IWebhookObserverSubscriber && cancellationToken.IsCancellationRequested)
            {
                return CancelledResult(plan.State.Partition, progress.LastHandled);
            }

            var outcome = await ApplyBatchResult(plan, progress, handleEventsResult, cancellationToken);
            if (outcome.Result is { } stopped) return stopped;
            if (outcome.Failure is { } failure) return await FailBatch(plan.State, progress, failure);

            progress.LastScanned = GetLastScanned(progress.LastScanned, plan.Events);
        }

        LogHandledAll(plan.State, progress.LastHandled);
        return JobStepResult.Succeeded(CreateResult(progress.LastHandled) with
        {
            LastScannedEventSequenceNumber = progress.LastScanned
        });
    }

    async Task<bool> IsClosedForResume(HandleEventsForPartitionState currentState) =>
        (currentState.EpisodeId is not null || _subscriber is IWebhookObserverSubscriber) &&
        !await _observer.CanResumeFailedPartition(currentState.Partition, currentState.EpisodeId, currentState.DeferralId);

    async Task<HandledBatch> HandleBatch(DeliveryPlan plan, CancellationToken cancellationToken) =>
        _subscriber is IWebhookObserverSubscriber or IAmOwnedByClient
            ? await TryHandleEvents(plan, cancellationToken)
            : await TransientTimeouts.RetryOnceOnTimeout(
                () => TryHandleEvents(plan, cancellationToken),
                TransientRetryDelay,
                timeout => logger.RetryingAfterTimeout(plan.State.Partition, TransientTimeouts.ShortMessageFor(timeout)),
                cancellationToken);

    async Task<BatchOutcome> ApplyBatchResult(DeliveryPlan plan, PerformProgress progress, HandledBatch handleEventsResult, CancellationToken cancellationToken)
    {
        if (handleEventsResult.TryGetException(out var handleEventsException))
        {
            var failure = FailureOf(handleEventsException);
            progress.LastAttempted = plan.Events.Current.First().Context.SequenceNumber;
            return BatchOutcome.Fail(failure);
        }

        if (handleEventsResult.TryGetResult(out var handledEventsResult))
        {
            var (eventObserverResult, handledEvents) = handledEventsResult;
            return await ApplyObserverResult(plan, progress, new(eventObserverResult, handledEvents), cancellationToken);
        }

        return BatchOutcome.Continue;
    }

    async Task<BatchOutcome> ApplyObserverResult(DeliveryPlan plan, PerformProgress progress, ObservedBatch batch, CancellationToken cancellationToken) =>
        batch.Result.State switch
        {
            ObserverSubscriberState.Ok => await ApplyOk(plan, progress, batch, cancellationToken),
            ObserverSubscriberState.Deferred => await ApplyDeferred(plan, progress, batch, cancellationToken),
            ObserverSubscriberState.Failed => await ApplyFailed(plan, progress, batch, cancellationToken),
            ObserverSubscriberState.Disconnected => ApplyDisconnected(plan, progress, batch),
            _ => BatchOutcome.Continue
        };

    async Task<BatchOutcome> ApplyOk(DeliveryPlan plan, PerformProgress progress, ObservedBatch batch, CancellationToken cancellationToken)
    {
        RecordSuccessfulObservation(plan.State);
        progress.LastAttempted = EventSequenceNumber.Unavailable;

        // Nothing was recorded for this batch when it is not committed, so it is neither checkpointed nor counted here: the next
        // run resumes from the last persisted checkpoint and counts it when it handles the batch again.
        return await CommitHandledEvents(plan, progress, batch, cancellationToken)
            ? BatchOutcome.Continue
            : BatchOutcome.Stop(ClosedResult(progress.LastHandled));
    }

    async Task<BatchOutcome> ApplyDeferred(DeliveryPlan plan, PerformProgress progress, ObservedBatch batch, CancellationToken cancellationToken)
    {
        if (batch.Result.HandledAnyEvents && !await CommitHandledEvents(plan, progress, batch, cancellationToken))
        {
            return BatchOutcome.Stop(ClosedResult(progress.LastHandled));
        }

        await ReportPartitionDeferred(plan.State, batch.Result.Deferral!);
        return BatchOutcome.Stop(JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(progress.LastHandled) with { Deferral = batch.Result.Deferral })));
    }

    async Task<BatchOutcome> ApplyFailed(DeliveryPlan plan, PerformProgress progress, ObservedBatch batch, CancellationToken cancellationToken)
    {
        var eventObserverResult = batch.Result;
        var handledCount = EventCount.Zero;
        if (eventObserverResult.LastSuccessfulObservation.IsActualValue)
        {
            handledCount = plan.Events.Current.Count(_ => _.Context.SequenceNumber <= eventObserverResult.LastSuccessfulObservation);
        }

        var failure = new BatchFailure(
            eventObserverResult.ExceptionMessages.ToArray(),
            eventObserverResult.ExceptionStackTrace,
            FailureKind.Handling,
            eventObserverResult.FailurePolicy);
        if (eventObserverResult.HandledAnyEvents)
        {
            var failedEvent = batch.HandledEvents.FirstOrDefault(e => e.Context.SequenceNumber > eventObserverResult.LastSuccessfulObservation);
            progress.LastAttempted = failedEvent is not null
                ? failedEvent.Context.SequenceNumber
                : eventObserverResult.LastSuccessfulObservation.Next();

            if (!await CommitHandledEvents(plan, progress, batch, cancellationToken))
            {
                return BatchOutcome.Stop(ClosedResult(progress.LastHandled));
            }
        }
        else
        {
            progress.LastAttempted = batch.HandledEvents[0].Context.SequenceNumber;
        }

        logger.FailedHandlingEvents(plan.State.Partition, handledCount, progress.LastAttempted, progress.LastHandled);
        return BatchOutcome.Fail(failure);
    }

    BatchOutcome ApplyDisconnected(DeliveryPlan plan, PerformProgress progress, ObservedBatch batch)
    {
        progress.LastAttempted = batch.HandledEvents[0].Context.SequenceNumber;
        logger.EventHandlerDisconnected(plan.State.Partition, progress.LastHandled);
        return BatchOutcome.Fail(new([SubscriberDisconnected], string.Empty, FailureKind.Disconnected));
    }

    async Task<bool> CommitHandledEvents(DeliveryPlan plan, PerformProgress progress, ObservedBatch batch, CancellationToken cancellationToken)
    {
        var lastSuccessfulObservation = batch.Result.LastSuccessfulObservation;
        if (!await ReportFromWork(lastSuccessfulObservation)) return false;
        progress.LastHandled = lastSuccessfulObservation;
        await ReportHandledEvents(plan.State.Partition, batch.HandledEvents, lastSuccessfulObservation, cancellationToken);
        return true;
    }

    async Task<JobStepResult> FailBatch(HandleEventsForPartitionState currentState, PerformProgress progress, BatchFailure failure)
    {
        var failedAt = progress.LastAttempted.IsActualValue ? progress.LastAttempted : currentState.StartEventSequenceNumber;
        if (failure.Kind == FailureKind.Timeout && _subscriber is not IWebhookObserverSubscriber)
        {
            // The observer is not told yet: it is the grain that was too busy to answer, and the job runs the
            // step again before it reports the partition. A webhook timeout is different: it is a delivery
            // failure, so it is reported below and retried only by the webhook retry policy.
            logger.StepTimedOut(failure.Messages[0]);
            return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                CreateResult(progress.LastHandled) with
                {
                    TransientFailure = new(currentState.Partition, failedAt, failure.Messages) { DeliveryFence = _deliveryFence }
                },
                failure.Messages,
                string.Empty));
        }

        await ReportPartitionFailed(currentState, failedAt, failure);
        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            CreateResult(progress.LastHandled),
            TransientTimeouts.ShortenForStep(failure.Messages),
            string.Empty));
    }

    void LogHandledAll(HandleEventsForPartitionState currentState, EventSequenceNumber lastHandled)
    {
        if (lastHandled == EventSequenceNumber.Unavailable)
        {
            using (logger.BeginObserverKeyScope(currentState.ObserverKey))
            {
                logger.HandledNoEvents(currentState.Partition);
            }
        }
        else
        {
            logger.HandledAllEvents(currentState.Partition, lastHandled);
        }
    }

    JobStepResult TimedOutResult(HandleEventsForPartitionState currentState, EventSequenceNumber lastHandled, Exception timeout)
    {
        // A grain call timing out is recorded once, briefly, and never escalated to the observer: the observer
        // is the grain that was too busy to answer.
        var message = TransientTimeouts.ShortMessageFor(timeout);
        logger.StepTimedOut(message);
        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            CreateResult(lastHandled) with
            {
                TransientFailure = new(currentState.Partition, NextFailedAt(currentState, lastHandled), [message])
            },
            [message],
            string.Empty));
    }

    async Task<Catch<JobStepResult>> FailedUnexpectedlyResult(HandleEventsForPartitionState currentState, EventSequenceNumber lastHandled, Exception e)
    {
        logger.FailedPerforming(e, nameof(HandleEventsForPartition));
        var failedAt = NextFailedAt(currentState, lastHandled);
        if (!await TryReportPartitionFailed(currentState, failedAt, e))
        {
            return UnreportedFailure(currentState.Partition, failedAt, lastHandled, e);
        }

        if (!lastHandled.IsActualValue)
        {
            return e;
        }

        logger.FailedWithPartialSuccess(e, lastHandled);
        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(lastHandled), e));
    }

    JobStepResult CancelledResult(Key partition, EventSequenceNumber lastHandled)
    {
        LogCancelled(lastHandled, partition);
        return ClosedResult(lastHandled);
    }

    JobStepResult ClosedResult(EventSequenceNumber lastSuccessfullyHandled) =>
        JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(lastSuccessfullyHandled)));

    void LogCancelled(EventSequenceNumber lastHandledSequenceNumber, Key partition)
    {
        if (!lastHandledSequenceNumber.IsActualValue)
        {
            logger.CancelledBeforeHandlingAnyEvents(partition);
        }
        else
        {
            logger.CancelledAfterHandlingEvents(partition, lastHandledSequenceNumber);
        }
    }

    ObserverSubscriberContext CreateSubscriberContext(ObserverSubscription subscription, HandleEventsForPartitionState currentState)
    {
        var deliveryTarget = subscription.SubscriberType.IsAssignableTo(typeof(IAmOwnedByClient))
            ? _deliveryTarget
            : subscriberSelector.Select(subscription, currentState.Partition);
        return new(deliveryTarget.ConnectedClient ?? subscription.Arguments)
        {
            ReplayEpoch = currentState.ReplayEpoch,
            Origin = currentState.EpisodeId is not null || currentState.DeferralId is not null
                ? ObservationOrigin.Retry
                : currentState.EventObservationState.ToObservationOrigin()
        };
    }

    async Task DrainAbandonedDeliveryBeforeHandling(ObserverSubscription subscription, HandleEventsForPartitionState currentState, CancellationToken cancellationToken)
    {
        _abandonedDeliveryHold = subscription.SubscriberType.IsAssignableTo(typeof(IAmOwnedByClient))
            ? await configurationProvider.GetAbandonedDeliveryHoldForObserver(currentState.ObserverKey)
            : TimeSpan.Zero;
        if (_abandonedDeliveryHold <= TimeSpan.Zero) return;
        var drain = new AbandonedDeliveryDrain(_grainFactory, _clock, logger);
        foreach (var fence in (await _observer.GetDeliveryFences()).Where(fence => fence.Partition == currentState.Partition))
        {
            await drain.Until(fence, cancellationToken);
            await _observer.ReleaseDeliveryFence(fence.Partition, fence.ReleaseBy);
        }
    }

    async Task<HandledBatch> TryHandleEvents(DeliveryPlan plan, CancellationToken cancellationToken)
    {
        try
        {
            var eventsToHandle = ObserverEventPreparation.Prepare(plan.State.EventObservationState, plan.Events.Current, plan.NonRedactionEventTypeIds, plan.Filters);
            if (eventsToHandle.Length != 0)
            {
                var decryptedEvents = await DecryptEvents(eventsToHandle);
                var subscriberContext = plan.Context
                    .WithDeadlines(plan.Timeout, _abandonedDeliveryHold, _clock);
                _deliveryFence = AbandonedDeliveryDrain.Capture(_deliverySubscription, plan.State.Partition, _deliveryTarget, subscriberContext);
                var deliveryCancellation = _subscriber is IWebhookObserverSubscriber ? cancellationToken : CancellationToken.None;
                var result = await _subscriber!.OnNextWithin(plan.Timeout, plan.State.Partition, decryptedEvents, subscriberContext, deliveryCancellation);
                return (result, decryptedEvents);
            }

            logger.NoMoreEventsToHandle(plan.State.Partition, plan.State.StartEventSequenceNumber, plan.State.EndEventSequenceNumber);
            return default(None);
        }
        catch (Exception ex)
        {
            if (TransientTimeouts.IsTimeout(ex) && _deliveryFence?.ReleaseBy is { } releaseBy)
            {
                await new AbandonedDeliveryDrain(_grainFactory, _clock, logger).Until(new(plan.State.Partition, releaseBy, _deliveryFence.Targets ?? []), CancellationToken.None);
            }
            logger.ErrorHandling(ex, plan.State.Partition);
            return ex;
        }
    }
}
