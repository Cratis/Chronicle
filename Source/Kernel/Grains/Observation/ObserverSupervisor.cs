// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation;


/// <summary>
/// Represents an implementation of <see cref="IObserverSupervisor"/>.
/// </summary>
/// <remarks>
/// This is a partial class. For structural, navigation and maintenance purposes, you'll find partial implementations
/// representing different aspects.
/// </remarks>
public partial class ObserverSupervisor : ObserverWorker, IObserverSupervisor
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ILogger<ObserverSupervisor> _logger;
    StreamSubscriptionHandle<AppendedEvent>? _streamSubscription;
    IAsyncStream<AppendedEvent>? _stream;
    ObserverId _observerId = Guid.Empty;
    ObserverKey _observerKey = ObserverKey.NotSet;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    MicroserviceId _sourceMicroserviceId = MicroserviceId.Unspecified;
    TenantId _sourceTenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    FailedPartitionSupervisor? _failedPartitionSupervisor;
    IDisposable? _recoverFailedPartitionsTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverSupervisor"/> class.
    /// </summary>
    /// <param name="observerState"><see cref="IPersistentState{T}"/> for the <see cref="ObserverState"/>.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public ObserverSupervisor(
        [PersistentState(nameof(ObserverState), ObserverState.StorageProvider)] IPersistentState<ObserverState> observerState,
        IExecutionContextManager executionContextManager,
        ILogger<ObserverSupervisor> logger) : base(observerState, logger)
    {
        _executionContextManager = executionContextManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override MicroserviceId MicroserviceId => _observerKey!.MicroserviceId;

    /// <inheritdoc/>
    protected override TenantId TenantId => _observerKey!.TenantId;

    /// <inheritdoc/>
    protected override EventSequenceId EventSequenceId => _observerKey!.EventSequenceId;

    /// <inheritdoc/>
    protected override MicroserviceId? SourceMicroserviceId => _observerKey!.SourceMicroserviceId;

    /// <inheritdoc/>
    protected override TenantId? SourceTenantId => _observerKey!.SourceTenantId;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);
        _eventSequenceId = _observerKey.EventSequenceId;
        _microserviceId = _observerKey.MicroserviceId;
        _tenantId = _observerKey.TenantId;
        _sourceMicroserviceId = _observerKey.SourceMicroserviceId ?? _microserviceId;
        _sourceTenantId = _observerKey.SourceTenantId ?? _tenantId;

        await ReadStateAsync();

        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        State.EventSequenceId = _eventSequenceId;

        _logger.Activating(_observerId, _eventSequenceId, _microserviceId, _tenantId, _sourceMicroserviceId, _sourceTenantId);

        _executionContextManager.Establish(_tenantId, CorrelationId.New(), _microserviceId);

        var streamProvider = this.GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        var microserviceAndTenant = new MicroserviceAndTenant(_sourceMicroserviceId, _sourceTenantId);
        var streamId = StreamId.Create(microserviceAndTenant, _eventSequenceId);
        _stream = streamProvider.GetStream<AppendedEvent>(streamId);
        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.Deactivating(_observerId, _eventSequenceId, _microserviceId, _tenantId, _sourceMicroserviceId, _sourceTenantId);

        await UnsubscribeStream();
        await StopAnyRunningCatchup();
        State.RunningState = ObserverRunningState.Disconnected;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> GetEventTypes() => Task.FromResult(State.EventTypes);

#pragma warning disable CA1721 // Property names should not match get methods
    /// <inheritdoc/>
    public Task<ObserverSubscription> GetCurrentSubscription() => Task.FromResult(CurrentSubscription);

#pragma warning restore CA1721 // Property names should not match get methods

    /// <inheritdoc/>
    public Task<ObserverState> GetCurrentState() => Task.FromResult(State);

    /// <inheritdoc/>
    public async Task NotifyCatchUpComplete(IEnumerable<FailedPartition> failedPartitions)
    {
        await ReadStateAsync();

        State.RunningState = ObserverRunningState.Active;
        await UnsubscribeStream();
        await WriteStateAsync();

        await SubscribeStream(HandleEventForPartitionedObserverWhenSubscribing);

        if (_failedPartitionSupervisor is not null)
        {
            foreach (var failedPartition in failedPartitions)
            {
                await _failedPartitionSupervisor.Fail(
                    failedPartition.Partition,
                    failedPartition.Tail,
                    failedPartition.Messages,
                    failedPartition.StackTrace,
                    failedPartition.Occurred);
            }
        }
    }

    /// <inheritdoc/>
    public async Task NotifyReplayComplete(IEnumerable<FailedPartition> failedPartitions)
    {
        await ReadStateAsync();

        State.RunningState = ObserverRunningState.Active;
        await WriteStateAsync();

        await UnsubscribeStream();
        await SubscribeStream(HandleEventForPartitionedObserverWhenSubscribing);

        if (_failedPartitionSupervisor is not null)
        {
            foreach (var failedPartition in failedPartitions)
            {
                await _failedPartitionSupervisor.Fail(
                    failedPartition.Partition,
                    failedPartition.Tail,
                    failedPartition.Messages,
                    failedPartition.StackTrace,
                    failedPartition.Occurred);
            }
        }
    }

    /// <inheritdoc/>
    public Task NotifyPartitionReplayComplete(EventSourceId eventSourceId, IEnumerable<FailedPartition> failedPartitions) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task NotifyFailedPartitionRecoveryComplete(EventSourceId partition, EventSequenceNumber lastProcessedEvent)
    {
        if (_failedPartitionSupervisor is null) return;

        await _failedPartitionSupervisor.AssessRecovery(partition, lastProcessedEvent);
        State.FailedPartitions = _failedPartitionSupervisor.GetState().FailedPartitions;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public override async Task PartitionFailed(EventSourceId partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace)
    {
        if (_failedPartitionSupervisor is null) return;

        await _failedPartitionSupervisor.Fail(
            partition,
            sequenceNumber,
            exceptionMessages,
            exceptionStackTrace,
            DateTimeOffset.UtcNow);

        State.FailedPartitions = _failedPartitionSupervisor.GetState().FailedPartitions;
        await WriteStateAsync();
    }

    void TryRecoveringAnyFailedPartitions()
    {
        _logger.TryRecoveringAnyFailedPartitions(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        _recoverFailedPartitionsTimer = RegisterTimer(
            (object state) =>
            {
                var task = _failedPartitionSupervisor!.TryRecoveringAnyFailedPartitions();
                _recoverFailedPartitionsTimer?.Dispose();
                return task;
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromHours(1));
    }

    Task StartCatchup() => GrainFactory.GetGrain<ICatchUp>(_observerId, keyExtension: _observerKey).Start(CurrentSubscription);

    Task StartReplay() => GrainFactory.GetGrain<IReplay>(_observerId, keyExtension: _observerKey).Start(CurrentSubscription);

    async Task StopAnyRunningCatchup()
    {
        if (State.RunningState != ObserverRunningState.CatchingUp)
        {
            await GrainFactory.GetGrain<ICatchUp>(_observerId, keyExtension: _observerKey).Stop();
            await ReadStateAsync();
        }
    }

#pragma warning disable CA1851 // Possible multiple enumerations of IEnumerable
    bool HasDefinitionChanged(IEnumerable<EventType> eventTypes) =>
        State.RunningState != ObserverRunningState.New &&
        (State.EventTypes.Count() != eventTypes.Count() ||
        !eventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(State.EventTypes.OrderBy(_ => _.Id.Value)));
#pragma warning restore CA1851 // Possible multiple enumerations of IEnumerable

    async Task SubscribeStream(Func<AppendedEvent, Task> handler)
    {
        _logger.SubscribingToStream(_observerId, _eventSequenceId, _microserviceId, _tenantId, (EventSequenceId)_stream!.StreamId.GetKeyAsString(), _stream!.StreamId.GetNamespace()!);

        // Get the next event sequence number for our event types and use as the next event sequence number
        _streamSubscription = await _stream!.SubscribeAsync(
            (@event, _) =>
            {
                if (State.EventTypes.Any(et => et == @event.Metadata.Type))
                {
                    _logger.EventReceived(@event.Metadata.Type.Id, _observerId, _eventSequenceId, _microserviceId, _tenantId);
                    return handler(@event);
                }

                return Task.CompletedTask;
            },
            new EventSequenceNumberToken(State.NextEventSequenceNumber));

        // Note: Warm up the stream. The internals of Orleans will only do the producer / consumer handshake after an event has gone through the
        // stream. Since our observers can perform replays & catch ups at startup, we can't wait till the first event appears.
        await _stream.WarmUp();
    }

    async Task UnsubscribeStream()
    {
        if (_streamSubscription is not null)
        {
            await _streamSubscription.UnsubscribeAsync();
            _streamSubscription = null;
        }
    }
}
