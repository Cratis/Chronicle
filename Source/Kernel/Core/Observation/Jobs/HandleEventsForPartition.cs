// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Metrics;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a step in a replay job.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HandleEventsForPartition"/> class.
/// </remarks>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="eventCompliance"><see cref="IEventCompliance"/> for decrypting PII event content before dispatching to subscribers.</param>
/// <param name="subscriberSelector"><see cref="IObserverSubscriberSelector"/> for selecting which connected client instance to deliver to.</param>
/// <param name="configurationProvider"><see cref="IConfigurationForObserverProvider"/> for getting the observer's subscriber timeout.</param>
/// <param name="observerServiceClient">The service client for confirming projection writes.</param>
/// <param name="logger">The logger.</param>
/// <param name="eventGenerationRelease">The pinned generation release boundary.</param>
/// <param name="timeProvider">The delivery fence clock.</param>
/// <param name="observerMeter">The <see cref="IMeter{T}"/> the observer metrics are recorded with.</param>
/// <param name="options">The kernel configuration, selecting the metric names.</param>
public partial class HandleEventsForPartition(
    [PersistentState(nameof(JobStepState), Cratis.Choreography.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForPartitionState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    IObserverSubscriberSelector subscriberSelector,
    IConfigurationForObserverProvider configurationProvider,
    IObserverServiceClient observerServiceClient,
    ILogger<HandleEventsForPartition> logger,
    IEventGenerationRelease? eventGenerationRelease = null,
    TimeProvider? timeProvider = null,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<Observer>? observerMeter = null,
    IOptions<ChronicleOptions>? options = null) : JobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult, HandleEventsForPartitionState>(state, throttle, logger), IHandleEventsForPartition
{
    const string SubscriberDisconnected = "Subscriber is disconnected";

    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    TimeSpan _abandonedDeliveryHold;
    DeliveryFenceContext? _deliveryFence;

    IEventSequenceStorage? _eventSequenceStorage;
    IObserver _observer = null!;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;
    IObserverSubscriber? _subscriber;
    ObserverSubscriberKey? _subscriberKey;
    IGrainFactory _grainFactory = null!;
    ObserverSubscriberTarget _deliveryTarget = null!;
    ObserverSubscription _deliverySubscription = ObserverSubscription.Unsubscribed;
    Dictionary<EventType, EventTypeSchema> _eventTypeSchemas = [];
    IEventTypesStorage? _eventTypes;
    EventType[] _generationPins = [];
    EventStoreName _generationEventStore = EventStoreName.NotSet;
    EventGenerationDelivery _generationDelivery;
    bool _isCollapsingProjection;

    /// <summary>
    /// Gets or sets how long to wait before a handling attempt that timed out is made again. Exposed for testing.
    /// </summary>
    internal TimeSpan TransientRetryDelay { get; set; } = TransientTimeouts.DefaultRetryDelay;

    /// <summary>
    /// Gets or sets the most events one subscriber call may carry. Exposed for testing.
    /// </summary>
    internal int MaxEventsPerBatch { get; set; } = BoundedEventCursor.DefaultMaximumEventsPerBatch;

    /// <inheritdoc/>
    /// <remarks>
    /// A projection that collapses several event sources onto one read model document is deliberately written out
    /// of order per document, so its sink write cannot be guarded on the read model's watermark and a redelivered
    /// batch would be applied twice. Checkpointing after every batch shrinks that window to the single batch the
    /// live delivery path already carries. It equalizes the window with the live path; it does not make the
    /// subscriber idempotent.
    /// </remarks>
    protected override bool CheckpointAfterEveryBatch => _isCollapsingProjection;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _grainFactory = GrainFactory;

        // Steps are read one by one from storage, each with its own copy of the event types its observer reads.
        State.EventTypes = EventTypeInterner.Intern(State.EventTypes);
        State.ReaderScope = EventTypeInterner.Intern(State.ReaderScope);

        if (State.IsPrepared && State.ObserverKey != ObserverKey.NotSet)
        {
            _observer = GrainFactory.GetGrain<IObserver>(State.ObserverKey);
            var subscription = await _observer.GetSubscription();
            _eventSourceId = State.Partition.ToString();

            // An unsubscribed observer reports a placeholder subscriber type that is not a grain interface, so
            // resolving it throws and the step can never activate - it would fail and retry forever instead of
            // reaching PerformStep, which reports the disconnection the job knows how to handle.
            if (subscription.IsSubscribed)
            {
                BindSubscriber(subscription, State.Partition);
            }
        }
        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This is the activation-side entry point. The step's own background work never calls it through the grain
    /// reference; it reaches the activation through TryInvokeOnActivation instead.
    /// </remarks>
    public async Task ReportNewSuccessfullyHandledEvent(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginJobStepScope(State);
        State.LastSuccessfullyHandledEventSequenceNumber = lastHandledEventSequenceNumber;
        var writeStateResult = await WriteCheckpointDebounced();
        if (writeStateResult.TryGetException(out var error))
        {
            logger.FailedToPersistSuccessfullyHandledEvent(error, lastHandledEventSequenceNumber);
            writeStateResult.RethrowError();
        }
    }

    /// <inheritdoc/>
    protected override ValueTask InitializeState(HandleEventsForPartitionArguments request)
    {
        State.EpisodeId = request.EpisodeId;
        State.DeferralId = request.DeferralId;
        State.ReplayEpoch = request.ReplayEpoch;
        State.ObserverKey = request.ObserverKey;
        State.EventObservationState = request.EventObservationState;
        State.EventTypes = EventTypeInterner.Intern(request.EventTypes);
        State.ReaderScope = EventTypeInterner.Intern(request.ReaderScope);
        State.Partition = request.Partition;
        State.StartEventSequenceNumber = request.StartEventSequenceNumber;
        State.EndEventSequenceNumber = request.EndEventSequenceNumber;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask<HandleEventsForPartitionResult?> CreateCancelledResultFromCurrentState(HandleEventsForPartitionState currentState) =>
        ValueTask.FromResult<HandleEventsForPartitionResult?>(new(currentState.LastSuccessfullyHandledEventSequenceNumber));

    /// <inheritdoc/>
    protected override async Task<Monads.Result<PrepareJobStepError>> PrepareStep(HandleEventsForPartitionArguments request)
    {
        try
        {
            logger.Preparing(request.Partition, request.StartEventSequenceNumber, request.EndEventSequenceNumber);
            _grainFactory = GrainFactory;
            _observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
            var subscription = await _observer.GetSubscription();
            _eventSourceId = request.Partition.ToString() ?? EventSourceId.Unspecified;

            if (subscription.IsSubscribed)
            {
                BindSubscriber(subscription, request.Partition);
                logger.SuccessfullyPrepared(request.Partition);
                return Result.Success<PrepareJobStepError>();
            }

            logger.PreparingStoppedUnsubscribed(request.Partition);
            return Result.Failed(PrepareJobStepError.CannotPrepare);
        }
        catch (Exception e)
        {
            logger.FailedPreparing(e, nameof(HandleEventsForPartition));
            return Result.Failed(PrepareJobStepError.UnexpectedErrorPreparing);
        }
    }

    /// <inheritdoc/>
    protected override Task<Catch<JobStepResult>> PerformStep(HandleEventsForPartitionState currentState, CancellationToken cancellationToken)
    {
        // Legacy truncated state cannot be re-prepared here. Let the normal terminal path notify the owning job.
        if (currentState.ObserverKey == ObserverKey.NotSet)
        {
            logger.MissingObserverKey(currentState.Id.JobId, currentState.Id.JobStepId);
            return Task.FromResult<Catch<JobStepResult>>(JobStepResult.Failed("The prepared job step state was missing its observer key"));
        }

        return PerformPreparedStep(currentState, cancellationToken);
    }

    static HandleEventsForPartitionResult CreateResult(EventSequenceNumber lastSuccessfullyHandled) => new(lastSuccessfullyHandled);

    void BindSubscriber(ObserverSubscription subscription, Key partition)
    {
        _subscriberKey = GetObserverSubscriberKey(subscription, partition);
        _subscriber = GrainFactory.GetGrain(subscription.SubscriberType, _subscriberKey) as IObserverSubscriber;
        _isCollapsingProjection = subscription.IsCollapsingProjection;
    }

    ObserverSubscriberKey GetObserverSubscriberKey(ObserverSubscription subscription, Key partition)
    {
        _grainFactory ??= GrainFactory;
        _deliverySubscription = subscription;
        var isWebhook = typeof(IWebhookObserverSubscriber).IsAssignableFrom(subscription.SubscriberType);
        _deliveryTarget = isWebhook
            ? new ObserverSubscriberTarget(SiloAddress.Zero)
            : subscriberSelector.Select(subscription, partition);
        var siloAddress = isWebhook
            ? SiloAddress.Zero
            : _deliveryTarget.SiloAddress;
        return subscription.GetSubscriberKeyFor(partition, siloAddress);
    }

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

    async Task<AppendedEvent[]> DecryptEvents(IEnumerable<AppendedEvent> events)
    {
        if (_generationDelivery == EventGenerationDelivery.Pinned)
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
}
