// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a step in a replay job that handles events in global event-sequence order.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="eventCompliance"><see cref="IEventCompliance"/> for decrypting PII event content before dispatching to subscribers.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for resolving subscriber grains off the activation thread.</param>
/// <param name="subscriberSelector"><see cref="IObserverSubscriberSelector"/> for selecting which connected client instance to deliver to.</param>
/// <param name="configurationProvider"><see cref="IConfigurationForObserverProvider"/> for getting the observer's subscriber timeout.</param>
/// <param name="observerServiceClient">The client that confirms buffered writes on every silo.</param>
/// <param name="options">The kernel configuration.</param>
/// <param name="logger">The logger.</param>
/// <param name="eventGenerationRelease">The pinned generation release boundary.</param>
/// <param name="timeProvider">The delivery fence clock.</param>
public class HandleEventsForObserver(
    [PersistentState(nameof(JobStepState), Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForObserverState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    IGrainFactory grainFactory,
    IObserverSubscriberSelector subscriberSelector,
    IConfigurationForObserverProvider configurationProvider,
    IObserverServiceClient observerServiceClient,
    IOptions<ChronicleOptions> options,
    ILogger<HandleEventsForObserver> logger,
    IEventGenerationRelease? eventGenerationRelease = null,
    TimeProvider? timeProvider = null) : JobStep<HandleEventsForObserverArguments, HandleEventsForPartitionResult, HandleEventsForObserverState>(state, throttle, logger), IHandleEventsForObserver
{
    const string SubscriberDisconnected = "Subscriber is disconnected";

    readonly IOptions<ChronicleOptions> _options = options;
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly SemaphoreSlim _flushGate = new(1, 1);
    TimeSpan _abandonedDeliveryHold;
    DeliveryFenceContext? _deliveryFence;
    IEventSequenceStorage? _eventSequenceStorage;
    IObserver _observer = null!;
    ObserverSubscription _subscription = ObserverSubscription.Unsubscribed;
    Dictionary<EventType, EventTypeSchema> _eventTypeSchemas = [];
    IEventTypesStorage? _eventTypes;
    EventType[] _generationPins = [];
    EventStoreName _generationEventStore = EventStoreName.NotSet;

    EventSequenceNumber _lastHandled = EventSequenceNumber.Unavailable;
    EventSequenceNumber _lastDurableCheckpoint = EventSequenceNumber.Unavailable;
    JobStepCheckpointDebounce _durableCheckpoints = new(1);
    IReadOnlyDictionary<SiloAddress, Guid[]>? _windows;
    Exception? _checkpointFailure;
    ReplayHandledEventsPage? _replayReports;

    /// <summary>
    /// Gets or sets how long to wait before a handling attempt that timed out is made again. Exposed for testing.
    /// </summary>
    internal TimeSpan TransientRetryDelay { get; set; } = TransientTimeouts.DefaultRetryDelay;

    bool DeliversPages { get; set; }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (State.IsPrepared)
        {
            _observer = GrainFactory.GetGrain<IObserver>(State.ObserverKey);
            _subscription = await _observer.GetSubscription();
        }
        await base.OnActivateAsync(cancellationToken);
        var jobs = ServiceProvider.GetService<IOptions<JobsOptions>>()?.Value;
        _durableCheckpoints = new(jobs?.StepCheckpointBatchInterval ?? 1);
        var interval = jobs?.StepCheckpointFlushInterval ?? TimeSpan.Zero;
        if (interval > TimeSpan.Zero)
        {
            this.RegisterGrainTimer(_ => FlushPendingCheckpoint(), new GrainTimerCreationOptions
            {
                DueTime = interval,
                Period = interval,
                Interleave = false
            });
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This is the activation-side entry point. The step's own background work never calls it through the grain
    /// reference; it reaches the activation through <see cref="ReportFromWork"/> instead.
    /// </remarks>
    public async Task<EventSequenceNumber> ReportNewSuccessfullyHandledEvent(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginJobStepScope(State);
        if (RecordHandled(lastHandledEventSequenceNumber))
        {
            await FlushCheckpoint();
        }

        return State.LastSuccessfullyHandledEventSequenceNumber;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This is the activation-side entry point, used by the flush timer. The step's own background work reaches the
    /// activation through <see cref="FlushFromWork"/> instead.
    /// </remarks>
    public async Task<EventSequenceNumber> FlushCheckpoint(bool start = false)
    {
        await _flushGate.WaitAsync();
        try
        {
            var capture = CaptureFlush(start);
            var windows = await ConfirmWindows(capture, error =>
            {
                _checkpointFailure = error;
                return Task.CompletedTask;
            });
            return await CommitFlush(capture, windows);
        }
        finally
        {
            _flushGate.Release();
        }
    }

    /// <inheritdoc/>
    protected override ValueTask InitializeState(HandleEventsForObserverArguments request)
    {
        State.ObserverKey = request.ObserverKey;
        State.EventObservationState = request.EventObservationState;
        State.EventTypes = request.EventTypes.ToArray();
        State.ReaderScope = request.ReaderScope;
        State.StartEventSequenceNumber = request.StartEventSequenceNumber;
        State.EndEventSequenceNumber = request.EndEventSequenceNumber;
        State.SkipFailedPartitions = request.SkipFailedPartitions;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask<HandleEventsForPartitionResult?> CreateCancelledResultFromCurrentState(HandleEventsForObserverState currentState) =>
        ValueTask.FromResult<HandleEventsForPartitionResult?>(new(currentState.LastSuccessfullyHandledEventSequenceNumber));

    /// <inheritdoc/>
    protected override async Task<Monads.Result<PrepareJobStepError>> PrepareStep(HandleEventsForObserverArguments request)
    {
        try
        {
            logger.Preparing(request.StartEventSequenceNumber, request.EndEventSequenceNumber);
            _observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
            _subscription = await _observer.GetSubscription();

            if (_subscription.IsSubscribed)
            {
                logger.SuccessfullyPrepared();
                return Result.Success<PrepareJobStepError>();
            }

            logger.PreparingStoppedUnsubscribed();
            return Result.Failed(PrepareJobStepError.CannotPrepare);
        }
        catch (Exception e)
        {
            logger.FailedPreparing(e, nameof(HandleEventsForObserver));
            return Result.Failed(PrepareJobStepError.UnexpectedErrorPreparing);
        }
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(HandleEventsForObserverState currentState, CancellationToken cancellationToken)
    {
        var lastSuccessfullyHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
        DeliversPages = currentState.EventObservationState == EventObservationState.Replay;
        _replayReports = DeliversPages ? new() : null;

        // PerformStep runs outside the activation. Only grain calls may access persistent State.
        _lastDurableCheckpoint = currentState.LastSuccessfullyHandledEventSequenceNumber;
        _subscription = await _observer.GetSubscription();
        try
        {
            lastSuccessfullyHandledEventSequenceNumber = currentState.LastSuccessfullyHandledEventSequenceNumber;
            if (!_subscription.IsSubscribed)
            {
                logger.PerformingStoppedUnsubscribed();
                return JobStepResult.Failed(SubscriberDisconnected, "This should have been ensured in the Prepare operation");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                logger.CancelledBeforeHandlingAnyEvents();
                return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
            }
            var eventSequenceStorage = GetEventSequenceStorage(
                currentState.ObserverKey.EventStore,
                currentState.ObserverKey.Namespace,
                currentState.ObserverKey.EventSequenceId);
            var scopedEventTypes = ObserverReaderScope
                .ForRequest(currentState.ReaderScope, currentState.EventTypes)
                .EventTypesToRead(currentState.EventTypes);
            var eventTypesToRead = scopedEventTypes?.ToArray()
                ?? await ResolveFallbackEventTypesToRead(_subscription.EventTypes);
            _generationPins = eventTypesToRead.ToArray();
            _generationEventStore = currentState.ObserverKey.EventStore;
            var nonRedactionEventTypeIds = eventTypesToRead
                .Where(et => et.Id != GlobalEventTypes.Redaction)
                .Select(et => et.Id)
                .ToHashSet();
            _eventTypes = storage.GetEventStore(currentState.ObserverKey.EventStore).EventTypes;
            _eventTypeSchemas = (await _eventTypes.GetFor(eventTypesToRead))
                .ToDictionary(_ => _.Type);

            // Baseline every window before dispatch: losing a silo before the first checkpoint must be detectable.
            if (!Advanced(await FlushFromWork(start: true)))
            {
                return ClosedResult();
            }

            using var events = await eventSequenceStorage.GetRange(
                currentState.LastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable
                    ? currentState.StartEventSequenceNumber
                    : currentState.LastSuccessfullyHandledEventSequenceNumber.Next(),
                currentState.EndEventSequenceNumber,
                eventTypes: eventTypesToRead,
                cancellationToken: cancellationToken);

            // A failed partition belongs to the retry job working on it. Reading the set once, before the walk,
            // keeps this step from asking the observer per batch; a partition that starts failing while the walk
            // is running is caught by the failure path below instead.
            HashSet<Key> failedPartitions = currentState.SkipFailedPartitions
                ? [.. await _observer.GetFailedPartitionKeys()]
                : [];

            var subscriberTimeout = await configurationProvider.GetSubscriberTimeoutForObserver(currentState.ObserverKey);
            await DrainAbandonedDeliveriesBeforeHandling(currentState.ObserverKey, cancellationToken);

            var lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
            var lastScannedEventSequenceNumber = EventSequenceNumber.Unavailable;
            while (await events.MoveNext())
            {
                var eventsRead = SetObservationStateIfSpecified(currentState.EventObservationState, events)
                    .OrderBy(_ => _.Context.SequenceNumber)
                    .ToArray();
                var eventsToHandle = FilterRedactedEventsForUnsubscribedTypes(eventsRead, nonRedactionEventTypeIds);
                eventsToHandle = _subscription.Filters.Apply(eventsToHandle);

                var usePages = DeliversPages &&
                    _subscription.SubscriberType.IsAssignableTo(typeof(IObserverPageSubscriber));
                if (usePages && failedPartitions.Count > 0)
                {
                    eventsToHandle = eventsToHandle.Where(_ => !failedPartitions.Contains(_.Context.EventSourceId)).ToArray();
                }

                foreach (var unit in usePages ? eventsToHandle.Chunk(_options.Value.ReadModels.MaxBatchSize) : Enumerable.Repeat(eventsToHandle, 1))
                {
                    if (usePages && cancellationToken.IsCancellationRequested)
                    {
                        if (!await FlushReplayReports()) return ClosedResult();
                        LogCancelled(lastSuccessfullyHandledEventSequenceNumber);
                        return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(_lastDurableCheckpoint)));
                    }

                    if (usePages)
                    {
                        var dispatched = false;
                        var pageResult = await TryHandleEvents(ObserverSubscriberKey.AllPartitions, unit, subscriberTimeout, page: true, onPageDispatch: () => dispatched = true);
                        if (dispatched && pageResult.TryGetException(out var pageException))
                        {
                            return await HandlePageDispatchException(unit[0], pageException);
                        }

                        if (pageResult.TryGetResult(out var pageHandled))
                        {
                            var (result, lastSuccessful) = await HandleSubscriberResult(
                                currentState,
                                unit[0].Context.EventSourceId,
                                pageHandled.Result,
                                pageHandled.HandledEvents,
                                lastSuccessfullyHandledEventSequenceNumber,
                                cancellationToken,
                                page: true);
                            lastSuccessfullyHandledEventSequenceNumber = lastSuccessful;
                            if (result is not null) return result;
                            if (!await FlushReplayReports()) return ClosedResult();
                            continue;
                        }

                        // Only preparation failures can fall back: a dispatched page may still be applying events,
                        // so re-sending its runs could apply them twice.
                    }

                    // Dispatch consecutive same-partition batches sequentially, in global sequence order, and return
                    // immediately on the first failure instead of continuing to later partitions. This is intentional:
                    // a projection that resolves keys, joins, or child collections across event sources depends on
                    // earlier partitions already being materialized in the sink, so processing past a failed partition
                    // would layer state onto a parent that never materialized and silently produce an inconsistent read
                    // model. Halting preserves the same global-order contract live observation relies on and surfaces
                    // the failure through PartitionFailed so it can be recovered before more state is built on top of it.
                    foreach (var partitionEvents in GetConsecutivePartitionBatches(unit))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            if (!await FlushReplayReports()) return ClosedResult();
                            LogCancelled(lastSuccessfullyHandledEventSequenceNumber);
                            return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(_lastDurableCheckpoint)));
                        }

                        var partition = partitionEvents[0].Context.EventSourceId;
                        if (failedPartitions.Contains(partition))
                        {
                            logger.SkippingFailedPartition(partition);
                            continue;
                        }

                        var handleEventsResult = await HandlePartitionEvents(partition, partitionEvents, subscriberTimeout, cancellationToken);
                        if (handleEventsResult.TryGetException(out var handleEventsException))
                        {
                            var exceptionMessages = handleEventsException.GetAllMessages().ToArray();
                            var exceptionStackTrace = handleEventsException.StackTrace ?? string.Empty;
                            lastEventSequenceNumberAttempted = partitionEvents[0].Context.SequenceNumber;
                            if (TransientTimeouts.IsTimeout(handleEventsException))
                            {
                                // The observer is not told yet: it is the grain that was too busy to answer, and the job runs
                                // the step again before it reports the partition.
                                var shortMessage = TransientTimeouts.ShortMessageFor(handleEventsException);
                                logger.StepTimedOut(shortMessage);
                                if (!await FlushReplayReports()) return ClosedResult();
                                if (!Advanced(await FlushFromWork()))
                                {
                                    return ClosedResult();
                                }

                                return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                                    CreateResult(_lastDurableCheckpoint) with
                                    {
                                        TransientFailure = new(partition, lastEventSequenceNumberAttempted, [shortMessage]) { DeliveryFence = _deliveryFence }
                                    },
                                    [shortMessage],
                                    string.Empty));
                            }

                            if (!await FlushReplayReports()) return ClosedResult();
                            await _observer.PartitionFailed(partition, lastEventSequenceNumberAttempted, exceptionMessages, exceptionStackTrace, handleEventsException.ToFailureKind());
                            if (!Advanced(await FlushFromWork()))
                            {
                                return ClosedResult();
                            }

                            return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                                CreateResult(_lastDurableCheckpoint),
                                TransientTimeouts.ShortenForStep(exceptionMessages),
                                string.Empty));
                        }
                        if (handleEventsResult.TryGetResult(out var handledEventsResult))
                        {
                            var (eventObserverResult, handledEvents) = handledEventsResult;
                            var (result, newLastSuccessfullyHandledEventSequenceNumber) = await HandleSubscriberResult(
                                currentState,
                                partition,
                                eventObserverResult,
                                handledEvents,
                                lastSuccessfullyHandledEventSequenceNumber,
                                cancellationToken);
                            lastSuccessfullyHandledEventSequenceNumber = newLastSuccessfullyHandledEventSequenceNumber;
                            if (result is not null)
                            {
                                return result;
                            }
                        }
                    }
                }

                if (!await FlushReplayReports()) return ClosedResult();

                if (eventsRead.Length != 0)
                {
                    lastScannedEventSequenceNumber = eventsRead[^1].Context.SequenceNumber;
                }
            }

            if (lastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable)
            {
                using (logger.BeginObserverKeyScope(currentState.ObserverKey))
                {
                    logger.HandledNoEvents();
                }
            }
            else
            {
                logger.HandledAllEvents(lastSuccessfullyHandledEventSequenceNumber);
            }

            if (!Advanced(await FlushFromWork()))
            {
                return ClosedResult();
            }

            return JobStepResult.Succeeded(CreateResult(_lastDurableCheckpoint) with
            {
                LastScannedEventSequenceNumber = lastScannedEventSequenceNumber
            });
        }
        catch (TaskCanceledException)
        {
            if (!await FlushReplayReportsAfterFailure()) return ClosedResult();
            LogCancelled(lastSuccessfullyHandledEventSequenceNumber);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(_lastDurableCheckpoint)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (!await FlushReplayReportsAfterFailure()) return ClosedResult();
            LogCancelled(lastSuccessfullyHandledEventSequenceNumber);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(_lastDurableCheckpoint)));
        }
        catch (Exception e) when (TransientTimeouts.IsTimeout(e))
        {
            if (!await FlushReplayReportsAfterFailure()) return ClosedResult();
            var message = TransientTimeouts.ShortMessageFor(e);
            logger.StepTimedOut(message);
            return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                CreateResult(_lastDurableCheckpoint),
                [message],
                string.Empty));
        }
        catch (Exception e)
        {
            if (!await FlushReplayReportsAfterFailure()) return ClosedResult();
            logger.FailedPerforming(e, nameof(HandleEventsForObserver));
            if (!lastSuccessfullyHandledEventSequenceNumber.IsActualValue)
            {
                return e;
            }

            logger.FailedWithPartialSuccess(e, lastSuccessfullyHandledEventSequenceNumber);
            return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(CreateResult(_lastDurableCheckpoint), e));
        }
    }

    static AppendedEvent[] SetObservationStateIfSpecified(EventObservationState eventObservationState, IEventCursor events)
    {
        if (eventObservationState != EventObservationState.None)
        {
            return events.Current.Select(@event =>
                @event with
                {
                    Context = @event.Context with
                    {
                        ObservationState = eventObservationState
                    }
                }).ToArray();
        }

        return events.Current.ToArray();
    }

    static AppendedEvent[] FilterRedactedEventsForUnsubscribedTypes(AppendedEvent[] events, HashSet<EventTypeId> nonRedactionEventTypeIds)
    {
        if (nonRedactionEventTypeIds.Count == 0)
        {
            return events;
        }

        var filtered = new AppendedEvent[events.Length];
        var count = 0;
        foreach (var @event in events)
        {
            if (@event.Context.EventType.Id != GlobalEventTypes.Redaction)
            {
                filtered[count++] = @event;
                continue;
            }

            if (@event.Content is not IDictionary<string, object?> contentDict || !contentDict.TryGetValue("originalEventType", out var originalEventTypeObj))
            {
                continue;
            }

            var originalEventTypeId = originalEventTypeObj?.ToString();
            if (originalEventTypeId is not null && nonRedactionEventTypeIds.Contains(new EventTypeId(originalEventTypeId)))
            {
                filtered[count++] = @event;
            }
        }

        return count == filtered.Length ? filtered : filtered[..count];
    }

    static IEnumerable<AppendedEvent[]> GetConsecutivePartitionBatches(AppendedEvent[] events)
    {
        var index = 0;
        while (index < events.Length)
        {
            var partition = events[index].Context.EventSourceId;
            var start = index;
            while (index < events.Length && events[index].Context.EventSourceId == partition)
            {
                index++;
            }

            yield return events[start..index];
        }
    }

    static HandleEventsForPartitionResult CreateResult(EventSequenceNumber lastSuccessfullyHandled) => new(lastSuccessfullyHandled);

    /// <summary>
    /// Records the handled position. Must run on the activation.
    /// </summary>
    /// <param name="lastHandled">The last handled event sequence number.</param>
    /// <returns>True when the batch interval is reached and the checkpoint must be flushed.</returns>
    /// <exception cref="ObserverFlushFailed">A previous flush failed and the failure is still retained.</exception>
    bool RecordHandled(EventSequenceNumber lastHandled)
    {
        _lastHandled = lastHandled;
        if (_durableCheckpoints.Report(CheckpointAfterEveryBatch))
        {
            return true;
        }

        if (_checkpointFailure is not null)
        {
            throw new ObserverFlushFailed();
        }

        return false;
    }

    /// <summary>
    /// Captures what a flush needs from the activation. Must run on the activation.
    /// </summary>
    /// <param name="start">Whether to establish the window baseline before handling a new range.</param>
    /// <returns>The <see cref="FlushCapture"/>.</returns>
    /// <exception cref="ObserverFlushFailed">A previous flush failed and the failure is still retained.</exception>
    FlushCapture CaptureFlush(bool start)
    {
        if (start)
        {
            _windows = null;
            _checkpointFailure = null;
            _lastHandled = State.LastSuccessfullyHandledEventSequenceNumber;
            _durableCheckpoints.Persisted();
        }

        if (_checkpointFailure is not null)
        {
            throw new ObserverFlushFailed();
        }

        // Capture the handled position before awaiting: later deliveries cannot be covered by this flush.
        return new(
            _lastHandled,
            new(_subscription.ObserverKey, ObserverType.Projection),
            State.EventObservationState == EventObservationState.Replay);
    }

    /// <summary>
    /// Confirms the buffered writes on every silo. Touches no activation state, so it can run outside the activation.
    /// </summary>
    /// <param name="capture">The <see cref="FlushCapture"/>.</param>
    /// <param name="onFailure">Retains a failure of the confirmation; awaited before the failure is rethrown.</param>
    /// <returns>The windows confirmed per silo.</returns>
    async Task<IReadOnlyDictionary<SiloAddress, Guid[]>> ConfirmWindows(FlushCapture capture, Func<Exception, Task> onFailure)
    {
        try
        {
            return await observerServiceClient.FlushAllFor(capture.Observer, capture.IsReplay);
        }
        catch (Exception error)
        {
            await onFailure(error);
            throw;
        }
    }

    /// <summary>
    /// Advances the durable checkpoint once the windows are confirmed. Must run on the activation.
    /// </summary>
    /// <param name="capture">The <see cref="FlushCapture"/> the windows were confirmed for.</param>
    /// <param name="windows">The windows confirmed per silo.</param>
    /// <returns>The last confirmed durable event sequence number.</returns>
    /// <exception cref="BulkWindowLost">A window confirmed earlier is no longer present on its silo.</exception>
    async Task<EventSequenceNumber> CommitFlush(FlushCapture capture, IReadOnlyDictionary<SiloAddress, Guid[]> windows)
    {
        try
        {
            if (_windows is not null && _windows.Any(previous =>
                previous.Value.Length > 0 && (!windows.TryGetValue(previous.Key, out var current) || previous.Value.Except(current).Any())))
            {
                throw new BulkWindowLost();
            }

            _windows = windows;
            var handled = capture.Handled;
            if (handled.IsActualValue && handled != State.LastSuccessfullyHandledEventSequenceNumber)
            {
                // Base timer, status and cancellation writes now see only confirmed durable progress.
                State.LastSuccessfullyHandledEventSequenceNumber = handled;
                var writeStateResult = await WriteStateAsync();
                if (writeStateResult.TryGetException(out var error))
                {
                    logger.FailedToPersistSuccessfullyHandledEvent(error, handled);
                    writeStateResult.RethrowError();
                }
            }

            // Progress reported while the flush was in flight is not covered by it and stays pending.
            if (handled == _lastHandled)
            {
                _durableCheckpoints.Persisted();
            }

            return State.LastSuccessfullyHandledEventSequenceNumber;
        }
        catch (Exception error)
        {
            _checkpointFailure = error;
            throw;
        }
    }

    /// <summary>
    /// Reports progress from the step's background work. Every touch of the activation goes through
    /// TryInvokeOnActivation.
    /// </summary>
    /// <param name="lastHandled">The last handled event sequence number.</param>
    /// <returns>The last confirmed durable event sequence number, or why the step refused a callback.</returns>
    async Task<Result<EventSequenceNumber, ActivationCallbackRejected>> ReportFromWork(EventSequenceNumber lastHandled)
    {
        var recorded = await TryInvokeOnActivation(() =>
        {
            using var scope = logger.BeginJobStepScope(State);
            return Task.FromResult(RecordHandled(lastHandled));
        });
        if (recorded.TryGetError(out var rejection))
        {
            return rejection;
        }

        if (recorded.TryGetResult(out var mustFlush) && mustFlush)
        {
            return await FlushFromWork();
        }

        return await TryInvokeOnActivation(() => Task.FromResult(State.LastSuccessfullyHandledEventSequenceNumber));
    }

    /// <summary>
    /// Flushes the checkpoint from the step's background work: capture on the activation, confirm the windows
    /// outside it, then commit on the activation. The remote call never runs under the step's gate, and the commit
    /// is refused, and so records nothing, when the run was stopped in between.
    /// </summary>
    /// <param name="start">Whether to establish the window baseline before handling a new range.</param>
    /// <returns>The last confirmed durable event sequence number, or why the step refused a callback.</returns>
    async Task<Result<EventSequenceNumber, ActivationCallbackRejected>> FlushFromWork(bool start = false)
    {
        await _flushGate.WaitAsync();
        try
        {
            var captured = await TryInvokeOnActivation(() =>
            {
                using var scope = logger.BeginJobStepScope(State);
                return Task.FromResult(CaptureFlush(start));
            });
            if (captured.TryGetError(out var rejection))
            {
                return rejection;
            }

            captured.TryGetResult(out var capture);
            var windows = await ConfirmWindows(
                capture,
                error => TryInvokeOnActivation(() =>
                {
                    _checkpointFailure = error;
                    return Task.CompletedTask;
                }));

            return await TryInvokeOnActivation(async () =>
            {
                using var scope = logger.BeginJobStepScope(State);
                return await CommitFlush(capture, windows);
            });
        }
        finally
        {
            _flushGate.Release();
        }
    }

    bool Advanced(Result<EventSequenceNumber, ActivationCallbackRejected> outcome)
    {
        if (outcome.TryGetResult(out var durable))
        {
            _lastDurableCheckpoint = durable;
            return true;
        }

        if (outcome.TryGetError(out var rejection))
        {
            logger.CheckpointCallbackRejected(rejection, _lastDurableCheckpoint);
        }

        return false;
    }

    JobStepResult ClosedResult() => JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(_lastDurableCheckpoint)));

    async Task FlushPendingCheckpoint()
    {
        if (!_durableCheckpoints.HasPendingCheckpoint || _checkpointFailure is not null)
        {
            return;
        }

        // A flush is already in flight and covers this progress, or the next tick will.
        if (!await _flushGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            var capture = CaptureFlush(start: false);
            var windows = await ConfirmWindows(capture, error =>
            {
                _checkpointFailure = error;
                return Task.CompletedTask;
            });
            await CommitFlush(capture, windows);
        }
        catch (Exception error)
        {
            // A timer cannot fail PerformStep directly. Retain the failure and reject the next report/final flush.
            logger.FailedToPersistSuccessfullyHandledEvent(error, _lastHandled);
        }
        finally
        {
            _flushGate.Release();
        }
    }

    async Task<(JobStepResult? Result, EventSequenceNumber LastSuccessfullyHandledEventSequenceNumber)> HandleSubscriberResult(
        HandleEventsForObserverState currentState,
        Key partition,
        ObserverSubscriberResult eventObserverResult,
        AppendedEvent[] handledEvents,
        EventSequenceNumber lastSuccessfullyHandledEventSequenceNumber,
        CancellationToken cancellationToken,
        bool page = false)
    {
        var handledCount = EventCount.Zero;
        if (eventObserverResult.LastSuccessfulObservation.IsActualValue)
        {
            handledCount = handledEvents.Count(_ => _.Context.SequenceNumber <= eventObserverResult.LastSuccessfulObservation);
        }

        switch (eventObserverResult.State)
        {
            case ObserverSubscriberState.Ok:
                if (!await RecordAndReportSubscriberEvents(partition, handledEvents, eventObserverResult.LastSuccessfulObservation, page, cancellationToken))
                {
                    // This batch is neither checkpointed nor counted here. The next run resumes from the last durable
                    // checkpoint, so it handles every batch since then again; the observer recognizes the reports it
                    // already took while it still remembers this step.
                    return (ClosedResult(), lastSuccessfullyHandledEventSequenceNumber);
                }

                return (null, eventObserverResult.LastSuccessfulObservation);
            case ObserverSubscriberState.Failed:
            case var _ when page:
                return await HandleFailedSubscriberResult(
                    currentState,
                    partition,
                    eventObserverResult,
                    handledEvents,
                    handledCount,
                    lastSuccessfullyHandledEventSequenceNumber,
                    cancellationToken,
                    page);
            case ObserverSubscriberState.Disconnected:
                var lastEventSequenceNumberAttempted = handledEvents[0].Context.SequenceNumber;
                logger.EventHandlerDisconnected(partition, lastSuccessfullyHandledEventSequenceNumber);
                if (!await FlushReplayReports()) return (ClosedResult(), lastSuccessfullyHandledEventSequenceNumber);
                await _observer.PartitionFailed(partition, lastEventSequenceNumberAttempted, [SubscriberDisconnected], string.Empty, FailureKind.Disconnected);
                if (!Advanced(await FlushFromWork()))
                {
                    return (ClosedResult(), lastSuccessfullyHandledEventSequenceNumber);
                }

                return (
                    JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                        CreateResult(_lastDurableCheckpoint),
                        [SubscriberDisconnected],
                        string.Empty)),
                    lastSuccessfullyHandledEventSequenceNumber);
            default:
                return (null, lastSuccessfullyHandledEventSequenceNumber);
        }
    }

    async Task<bool> RecordAndReportHandledEvents(Key partition, AppendedEvent[] handledEvents, EventSequenceNumber handledUpTo, CancellationToken cancellationToken)
    {
        var counts = handledEvents
            .Where(e => e.Context.SequenceNumber <= handledUpTo)
            .CountByEventType();
        var report = new HandledEventsReport(partition, counts, JobStepId, handledUpTo);
        if (_replayReports is not null)
        {
            _replayReports.Add(report);
            return true;
        }

        if (!Advanced(await ReportFromWork(handledUpTo))) return false;
        await TransientTimeouts.ReportHandledEventsWithoutFailing(_observer, report, TransientRetryDelay, logger, cancellationToken);
        return true;
    }

    async Task<bool> FlushReplayReportsAfterFailure()
    {
        try
        {
            return await FlushReplayReports();
        }
        catch (Exception exception)
        {
            logger.FailedPerforming(exception, nameof(HandleEventsForObserver));
            return true;
        }
    }

    async Task<bool> FlushReplayReports()
    {
        if (_replayReports?.LastHandled.IsActualValue != true) return true;
        var lastHandled = _replayReports.LastHandled;
        var reports = _replayReports.Reports;
        _replayReports.Clear();
        if (!Advanced(await ReportFromWork(lastHandled))) return false;

        // Events already handled before a stop still need their counts reported; cancellation only stops dispatch.
        await TransientTimeouts.ReportHandledEventsBatchWithoutFailing(
            _observer, JobStepId, reports, TransientRetryDelay, logger, CancellationToken.None);
        return true;
    }

    async Task<(JobStepResult Result, EventSequenceNumber LastSuccessfullyHandledEventSequenceNumber)> HandleFailedSubscriberResult(
        HandleEventsForObserverState currentState,
        Key partition,
        ObserverSubscriberResult eventObserverResult,
        AppendedEvent[] handledEvents,
        EventCount handledCount,
        EventSequenceNumber lastSuccessfullyHandledEventSequenceNumber,
        CancellationToken cancellationToken,
        bool page = false)
    {
        var lastEventSequenceNumberAttempted = EventSequenceNumber.Unavailable;
        if (eventObserverResult.HandledAnyEvents)
        {
            var failedEvent = handledEvents.FirstOrDefault(e => e.Context.SequenceNumber > eventObserverResult.LastSuccessfulObservation);
            lastEventSequenceNumberAttempted = failedEvent is not null
                ? failedEvent.Context.SequenceNumber
                : eventObserverResult.LastSuccessfulObservation.Next();

            if (!await RecordAndReportSubscriberEvents(partition, handledEvents, eventObserverResult.LastSuccessfulObservation, page, cancellationToken))
            {
                return (ClosedResult(), lastSuccessfullyHandledEventSequenceNumber);
            }

            lastSuccessfullyHandledEventSequenceNumber = eventObserverResult.LastSuccessfulObservation;
        }
        else
        {
            lastEventSequenceNumberAttempted = handledEvents[0].Context.SequenceNumber;
        }

        if (page && eventObserverResult.FailedAt.IsActualValue && eventObserverResult.FailedPartition is not null)
        {
            partition = eventObserverResult.FailedPartition;
            lastEventSequenceNumberAttempted = eventObserverResult.FailedAt;
        }

        logger.FailedHandlingEvents(partition, handledCount, lastEventSequenceNumberAttempted, lastSuccessfullyHandledEventSequenceNumber);
        var failedAt = lastEventSequenceNumberAttempted.IsActualValue ? lastEventSequenceNumberAttempted : currentState.StartEventSequenceNumber;
        if (!await FlushReplayReports()) return (ClosedResult(), lastSuccessfullyHandledEventSequenceNumber);
        await _observer.PartitionFailed(partition, failedAt, eventObserverResult.ExceptionMessages, eventObserverResult.ExceptionStackTrace, FailureKind.Handling, failurePolicy: eventObserverResult.FailurePolicy);
        if (!Advanced(await FlushFromWork()))
        {
            return (ClosedResult(), lastSuccessfullyHandledEventSequenceNumber);
        }

        return (
            JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
                CreateResult(_lastDurableCheckpoint),
                eventObserverResult.ExceptionMessages,
                eventObserverResult.ExceptionStackTrace)),
            lastSuccessfullyHandledEventSequenceNumber);
    }

    async Task<JobStepResult> HandlePageDispatchException(AppendedEvent firstEvent, Exception exception)
    {
        var messages = TransientTimeouts.IsTimeout(exception)
            ? [TransientTimeouts.ShortMessageFor(exception)]
            : TransientTimeouts.ShortenForStep(exception.GetAllMessages().ToArray());
        if (TransientTimeouts.IsTimeout(exception)) logger.StepTimedOut(messages[0]);
        if (!await FlushReplayReports()) return ClosedResult();
        if (!Advanced(await FlushFromWork())) return ClosedResult();

        // A page has no authoritative progress after an exception. Resume from the durable checkpoint,
        // just as for an exhausted per-run timeout, without counting or immediately re-dispatching it.
        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            CreateResult(_lastDurableCheckpoint) with
            {
                TransientFailure = new(firstEvent.Context.EventSourceId, firstEvent.Context.SequenceNumber, messages)
            },
            messages,
            string.Empty));
    }

    async Task DrainAbandonedDeliveriesBeforeHandling(ObserverKey observerKey, CancellationToken cancellationToken)
    {
        _abandonedDeliveryHold = _subscription.SubscriberType.IsAssignableTo(typeof(IAmOwnedByClient))
            ? await configurationProvider.GetAbandonedDeliveryHoldForObserver(observerKey)
            : TimeSpan.Zero;
        if (_abandonedDeliveryHold <= TimeSpan.Zero) return;
        var drain = new AbandonedDeliveryDrain(grainFactory, _clock, logger);
        foreach (var fence in await _observer.GetDeliveryFences())
        {
            await drain.Until(fence, cancellationToken);
            await _observer.ReleaseDeliveryFence(fence.Partition, fence.ReleaseBy);
        }
    }

    Task<Catch<(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents), None>> HandlePartitionEvents(Key partition, AppendedEvent[] events, TimeSpan subscriberTimeout, CancellationToken cancellationToken) =>
        _subscription.SubscriberType.IsAssignableTo(typeof(IAmOwnedByClient))
            ? TryHandleEvents(partition, events, subscriberTimeout)
            : TransientTimeouts.RetryOnceOnTimeout(
                () => TryHandleEvents(partition, events, subscriberTimeout),
                TransientRetryDelay,
                timeout => logger.RetryingAfterTimeout(partition, TransientTimeouts.ShortMessageFor(timeout)),
                cancellationToken);

    async Task<Catch<(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents), None>> TryHandleEvents(
        Key partition,
        AppendedEvent[] events,
        TimeSpan subscriberTimeout,
        bool page = false,
        Action? onPageDispatch = null)
    {
        try
        {
            var decryptedEvents = await DecryptEvents(events);
            var target = subscriberSelector.Select(_subscription, partition);

            // PerformStep (and everything it calls, including this method) runs off the grain's activation
            // thread - see the <remarks> on JobStep.PerformStep. The ambient Grain.GrainFactory property
            // validates the Orleans activation context on the calling thread and throws "Activation access
            // violation" when accessed here, so an explicitly injected IGrainFactory (a plain thread-safe
            // service) is used instead of the ambient property.
            var subscriber = grainFactory.GetGrain(_subscription.SubscriberType, _subscription.GetSubscriberKeyFor(partition, target.SiloAddress)) as IObserverSubscriber;
            var subscriberContext = new ObserverSubscriberContext(target.ConnectedClient ?? _subscription.Arguments).WithDeadlines(subscriberTimeout, _abandonedDeliveryHold, _clock);
            _deliveryFence = AbandonedDeliveryDrain.Capture(_subscription, partition, target, subscriberContext);
            if (page) onPageDispatch?.Invoke();
            var result = page
                ? await ((IObserverPageSubscriber)subscriber!).OnNextPageWithin(subscriberTimeout, decryptedEvents, subscriberContext)
                : await subscriber!.OnNextWithin(subscriberTimeout, partition, decryptedEvents, subscriberContext);
            return (result, decryptedEvents);
        }
        catch (Exception ex)
        {
            if (TransientTimeouts.IsTimeout(ex) && _deliveryFence?.ReleaseBy is { } releaseBy)
            {
                await new AbandonedDeliveryDrain(grainFactory, _clock, logger).Until(new(partition, releaseBy, _deliveryFence.Targets ?? []), CancellationToken.None);
            }
            logger.ErrorHandling(ex, partition);
            return ex;
        }
    }

    async Task<bool> RecordAndReportSubscriberEvents(
        Key partition,
        AppendedEvent[] events,
        EventSequenceNumber handledUpTo,
        bool page,
        CancellationToken cancellationToken)
    {
        if (!page)
        {
            return await RecordAndReportHandledEvents(partition, events, handledUpTo, cancellationToken);
        }

        foreach (var group in events.Where(_ => _.Context.SequenceNumber <= handledUpTo).GroupBy(_ => _.Context.EventSourceId))
        {
            var handled = group.ToArray();
            if (!await RecordAndReportHandledEvents(group.Key, handled, handled[^1].Context.SequenceNumber, cancellationToken)) return false;
        }

        return true;
    }

    void LogCancelled(EventSequenceNumber lastHandledSequenceNumber)
    {
        if (!lastHandledSequenceNumber.IsActualValue)
        {
            logger.CancelledBeforeHandlingAnyEvents();
        }
        else
        {
            logger.CancelledAfterHandlingEvents(lastHandledSequenceNumber);
        }
    }

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

    async Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events)
    {
        if (_subscription.GenerationDelivery == EventGenerationDelivery.Pinned)
        {
            return await (eventGenerationRelease ?? throw new EventGenerationDeliveryNotSupported()).Release(_generationEventStore, _generationPins, _eventTypeSchemas, events);
        }

        // The schemas read up front are those of the generations the observer subscribes to, while the events carry the
        // generation they were stored at - the schema of that generation is what says what to decrypt.
        var eventsToDecrypt = events as AppendedEvent[] ?? events.ToArray();
        if (_eventTypes is not null)
        {
            await _eventTypes.EnsureSchemasFor(_eventTypeSchemas, eventsToDecrypt);
        }

        return await eventCompliance.Release(eventsToDecrypt, _eventTypeSchemas);
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

    sealed record FlushCapture(EventSequenceNumber Handled, ObserverDetails Observer, bool IsReplay);
}
