// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserver"/>.
/// </summary>
/// <remarks>
/// This is a partial class. For structural, navigation and maintenance purposes, you'll find partial implementations
/// representing different aspects.
/// </remarks>
[StorageProvider(ProviderName = ObserverState.StorageProvider)]
public partial class Observer : Grain<ObserverState>, IObserver, IRemindable
{
    /// <summary>
    /// The name of the recover reminder.
    /// </summary>
    public const string RecoverReminder = "observer-failure-recovery";
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProviderProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly ILogger<Observer> _logger;
    readonly Dictionary<EventSourceId, StreamSubscriptionHandle<AppendedEvent>> _streamSubscriptionsByEventSourceId = new();
    StreamSubscriptionHandle<AppendedEvent>? _streamSubscription;
    IAsyncStream<AppendedEvent>? _stream;
    ObserverId _observerId = Guid.Empty;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    MicroserviceId _sourceMicroserviceId = MicroserviceId.Unspecified;
    TenantId _sourceTenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IGrainReminder? _recoverReminder;
    Type _subscriberType;

    IEventSequenceStorageProvider EventSequenceStorageProvider
    {
        get
        {
            // TODO: This is a temporary work-around till we fix #264 & #265
            _executionContextManager.Establish(_sourceTenantId, CorrelationId.New(), _sourceMicroserviceId);
            return _eventSequenceStorageProviderProvider();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Observer"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProviderProvider"><see creF="IEventSequenceStorageProvider"/> for working with the underlying event sequence.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public Observer(
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProviderProvider,
        IExecutionContextManager executionContextManager,
        ILogger<Observer> logger)
    {
        _eventSequenceStorageProviderProvider = eventSequenceStorageProviderProvider;
        _executionContextManager = executionContextManager;
        _logger = logger;
        _subscriberType = typeof(IObserverSubscriber);
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);

        var key = ObserverKey.Parse(keyAsString);
        _eventSequenceId = key.EventSequenceId;
        State.EventSequenceId = _eventSequenceId;
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;
        _sourceMicroserviceId = key.SourceMicroserviceId ?? _microserviceId;
        _sourceTenantId = key.SourceTenantId ?? _tenantId;

        _logger.Activating(_observerId, _eventSequenceId, _microserviceId, _tenantId, _sourceMicroserviceId, _sourceTenantId);

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

        State.RunningState = ObserverRunningState.Disconnected;
        await WriteStateAsync();
        await UnsubscribeStream();

        if (_recoverReminder is not null)
        {
            await UnregisterReminder(_recoverReminder);
        }
    }

    /// <inheritdoc/>
    public async Task SetMetadata(ObserverName name, ObserverType type)
    {
        State.Name = name;
        State.Type = type;

        await WriteStateAsync();
    }

    bool HasDefinitionChanged(IEnumerable<EventType> eventTypes) =>
        State.RunningState != ObserverRunningState.New &&
        !State.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(eventTypes.OrderBy(_ => _.Id.Value));

    async Task HandleEventForPartitionedObserver(AppendedEvent @event, bool setLastHandled = false)
    {
        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        try
        {
            if (State.IsDisconnected)
            {
                return;
            }

            var next = State.NextEventSequenceNumber;
            if (State.IsPartitionFailed(@event.Context.EventSourceId))
            {
                next = State.GetFailedPartition(@event.Context.EventSourceId).SequenceNumber;
            }

            if (@event.Metadata.SequenceNumber < next)
            {
                return;
            }

            var key = new ObserverSubscriberKey(_microserviceId, _tenantId, _eventSequenceId, @event.Context.EventSourceId, _sourceMicroserviceId, _sourceTenantId);
            var subscriber = (GrainFactory.GetGrain(_subscriberType, _observerId, key) as IObserverSubscriber)!;
            var result = await subscriber.OnNext(@event);
            if (result.State == ObserverSubscriberState.Error)
            {
                failed = true;
                exceptionMessages = result.ExceptionMessages;
                exceptionStackTrace = result.ExceptionStackTrace;
            }
            else if (result.State == ObserverSubscriberState.Disconnected)
            {
                await Unsubscribe();
                return;
            }
            else
            {
                State.NextEventSequenceNumber = @event.Metadata.SequenceNumber + 1;
                await WriteStateAsync();

                if (setLastHandled)
                {
                    State.LastHandled = @event.Metadata.SequenceNumber;
                }

                var nextSequenceNumber = await EventSequenceStorageProvider.GetTailSequenceNumber(State.EventSequenceId, State.EventTypes);

                if (State.NextEventSequenceNumber == nextSequenceNumber + 1 && State.RunningState != ObserverRunningState.Active)
                {
                    State.RunningState = ObserverRunningState.Active;
                    _logger.Active(_observerId, _microserviceId, _eventSequenceId, _tenantId);
                }
                await WriteStateAsync();
            }
        }
        catch (Exception ex)
        {
            failed = true;
            exceptionMessages = ex.GetAllMessages().ToArray();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
        }

        if (failed)
        {
            _logger.PartitionFailed(
                @event.Context.EventSourceId,
                @event.Context.SequenceNumber,
                _observerId,
                _eventSequenceId,
                _microserviceId,
                _tenantId,
                _sourceMicroserviceId,
                _sourceTenantId);

            State.FailPartition(
                @event.Context.EventSourceId,
                @event.Metadata.SequenceNumber,
                exceptionMessages.ToArray(),
                exceptionStackTrace);

            await WriteStateAsync();
            await HandleReminderRegistration();
        }
    }

    async Task SubscribeStream(Func<AppendedEvent, Task> handler)
    {
        _logger.SubscribingToStream(_observerId, _eventSequenceId, _microserviceId, _tenantId, _stream!.Guid, _stream!.Namespace);

        _streamSubscription = await _stream!.SubscribeAsync(
            (@event, _) =>
            {
                _logger.EventReceived(@event.Metadata.Type.Id, _observerId, _eventSequenceId, _microserviceId, _tenantId);
                return handler(@event);
            },
            State.EventTypes.Any() ? new EventSequenceNumberTokenWithFilter(State.NextEventSequenceNumber, State.EventTypes) : new EventSequenceNumberToken(State.NextEventSequenceNumber));

        // Note: Add a warm up event. The internals of Orleans will only do the producer / consumer handshake after an event has gone through the
        // stream. Since our observers can perform replays & catch ups at startup, we can't wait till the first event appears.
        var @event = new AppendedEvent(
            new(EventSequenceNumber.WarmUp, new EventType(EventTypeId.Unknown, 1)),
            new(
                EventSourceId.Unspecified,
                EventSequenceNumber.WarmUp,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                _tenantId,
                CorrelationId.New(),
                CausationId.System,
                CausedBy.System,
                EventObservationState.Initial),
            new ExpandoObject());
        await _stream!.OnNextAsync(@event, new EventSequenceNumberToken());
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
