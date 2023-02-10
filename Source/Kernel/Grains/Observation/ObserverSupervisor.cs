// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;
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
public partial class ObserverSupervisor : ObserverWorker, IObserverSupervisor, IRemindable
{
    /// <summary>
    /// The name of the recover reminder.
    /// </summary>
    public const string RecoverReminder = "observer-failure-recovery";

    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProviderProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly ILogger<ObserverSupervisor> _logger;
    readonly Dictionary<EventSourceId, StreamSubscriptionHandle<AppendedEvent>> _streamSubscriptionsByEventSourceId = new();
    StreamSubscriptionHandle<AppendedEvent>? _streamSubscription;
    IAsyncStream<AppendedEvent>? _stream;
    ObserverId _observerId = Guid.Empty;
    ObserverKey _observerKey = ObserverKey.NotSet;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    MicroserviceId _sourceMicroserviceId = MicroserviceId.Unspecified;
    TenantId _sourceTenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IGrainReminder? _recoverReminder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverSupervisor"/> class.
    /// </summary>
    /// <param name="observerState"><see cref="IPersistentState{T}"/> for the <see cref="ObserverState"/>.</param>
    /// <param name="eventSequenceStorageProviderProvider"><see creF="IEventSequenceStorageProvider"/> for working with the underlying event sequence.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public ObserverSupervisor(
        [PersistentState(nameof(ObserverState), ObserverState.StorageProvider)] IPersistentState<ObserverState> observerState,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProviderProvider,
        IExecutionContextManager executionContextManager,
        ILogger<ObserverSupervisor> logger) : base(executionContextManager, eventSequenceStorageProviderProvider, observerState, logger)
    {
        _eventSequenceStorageProviderProvider = eventSequenceStorageProviderProvider;
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
    public override async Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);

        _observerKey = ObserverKey.Parse(keyAsString);
        _eventSequenceId = _observerKey.EventSequenceId;
        State.EventSequenceId = _eventSequenceId;
        _microserviceId = _observerKey.MicroserviceId;
        _tenantId = _observerKey.TenantId;
        _sourceMicroserviceId = _observerKey.SourceMicroserviceId ?? _microserviceId;
        _sourceTenantId = _observerKey.SourceTenantId ?? _tenantId;

        _logger.Activating(_observerId, _eventSequenceId, _microserviceId, _tenantId, _sourceMicroserviceId, _sourceTenantId);

        _executionContextManager.Establish(_tenantId, CorrelationId.New(), _microserviceId);

        var streamProvider = GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        var microserviceAndTenant = new MicroserviceAndTenant(_sourceMicroserviceId, _sourceTenantId);
        _stream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, microserviceAndTenant);

        _recoverReminder = await GetReminder(RecoverReminder);
        await HandleReminderRegistration();
        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync()
    {
        _logger.Deactivating(_observerId, _eventSequenceId, _microserviceId, _tenantId, _sourceMicroserviceId, _sourceTenantId);

        await UnsubscribeStream();

        if (_recoverReminder is not null)
        {
            await UnregisterReminder(_recoverReminder);
        }

        await StopAnyRunningCatchup();
        State.RunningState = ObserverRunningState.Disconnected;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task SetNameAndType(ObserverName name, ObserverType type)
    {
        State.Name = name;
        State.Type = type;

        await WriteStateAsync();
    }

    #pragma warning disable CA1721 // Property names should not match get methods
    /// <inheritdoc/>
    public Task<ObserverSubscription> GetCurrentSubscription() => Task.FromResult(CurrentSubscription);
    #pragma warning restore CA1721 // Property names should not match get methods

    /// <inheritdoc/>
    public async Task NotifyCatchUpComplete()
    {
        await ReadStateAsync();

        State.RunningState = ObserverRunningState.Active;
        await WriteStateAsync();

        await SubscribeStream(HandleEventForPartitionedObserverWhenSubscribing);
    }

    /// <inheritdoc/>
    public Task NotifyFailedPartitionRecoveryComplete(EventSequenceNumber lastProcessedEvent) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task PartitionFailed(AppendedEvent @event, IEnumerable<string> exceptionMessages, string exceptionStackTrace)
    {
        State.FailPartition(
            @event.Context.EventSourceId,
            @event.Metadata.SequenceNumber,
            exceptionMessages.ToArray(),
            exceptionStackTrace);

        await WriteStateAsync();
        await HandleReminderRegistration();
    }

    Task StartCatchup() => GrainFactory.GetGrain<ICatchUp>(_observerId, keyExtension: _observerKey).Start(CurrentSubscription);

    async Task StopAnyRunningCatchup()
    {
        if (State.RunningState != ObserverRunningState.CatchingUp)
        {
            await GrainFactory.GetGrain<ICatchUp>(_observerId, keyExtension: _observerKey).Stop();
            await ReadStateAsync();
        }
    }

    bool HasDefinitionChanged(IEnumerable<EventType> eventTypes) =>
        State.RunningState != ObserverRunningState.New &&
        !State.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(eventTypes.OrderBy(_ => _.Id.Value));

    async Task SubscribeStream(Func<AppendedEvent, Task> handler)
    {
        _logger.SubscribingToStream(_observerId, _eventSequenceId, _microserviceId, _tenantId, _stream!.Guid, _stream!.Namespace);

        // Get the next event sequence number for our event types and use as the next event sequence number
        _streamSubscription = await _stream!.SubscribeAsync(
            (@event, _) =>
            {
                _logger.EventReceived(@event.Metadata.Type.Id, _observerId, _eventSequenceId, _microserviceId, _tenantId);
                return handler(@event);
            },
            new EventSequenceNumberToken(State.NextEventSequenceNumber),
            ObserverFilters.EventTypesFilter,
            State.EventTypes.ToArray());

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
