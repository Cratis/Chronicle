// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventLogs;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserver"/>.
/// </summary>
[StorageProvider(ProviderName = ObserverState.StorageProvider)]
public class Observer : Grain<ObserverState>, IObserver
{
    readonly IRequestContextManager _requestContextManager;
    readonly ILogger<Observer> _logger;
    StreamSubscriptionHandle<AppendedEvent>? _subscription;
    IAsyncStream<AppendedEvent>? _stream;
    ObserverId _observerId = Guid.Empty;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observer"/> class.
    /// </summary>
    /// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with the Orleans request context.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public Observer(
        IRequestContextManager requestContextManager,
        ILogger<Observer> logger)
    {
        _requestContextManager = requestContextManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverKey.Parse(keyAsString);
        _eventSequenceId = key.EventSequenceId;
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;

        var streamProvider = GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        var microserviceAndTenant = new MicroserviceAndTenant(_microserviceId, _tenantId);
        _stream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, microserviceAndTenant);

        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task Rewind()
    {
        _logger.Rewinding(_microserviceId, _eventSequenceId, _tenantId);
        State.Offset = 0;
        await Unsubscribe();
        await Subscribe(State.EventTypes);
    }

    /// <inheritdoc/>
    public async Task Subscribe(IEnumerable<EventType> eventTypes)
    {
        _logger.Subscribing(_microserviceId, _eventSequenceId, _tenantId);
        _subscription?.UnsubscribeAsync();

        if (HasDefinitionChanged(eventTypes))
        {
            State.Offset = 0;
        }

        State.EventTypes = eventTypes;
        await WriteStateAsync();

        var connectionId = _requestContextManager.Get(RequestContextKeys.ConnectionId).ToString()!;
        _subscription = await _stream!.SubscribeAsync(
            async (@event, _) =>
            {
                var key = new PartitionedObserverKey(_microserviceId, _tenantId, _eventSequenceId, @event.Context.EventSourceId);
                var partitionedObserver = GrainFactory.GetGrain<IPartitionedObserver>(_observerId, keyExtension: key);
                try
                {
                    await partitionedObserver.SetConnectionId(connectionId);
                    await partitionedObserver.OnNext(@event, eventTypes);

                    State.Offset = @event.Metadata.SequenceNumber + 1;
                    State.LastHandled = @event.Metadata.SequenceNumber + 1;
                    await WriteStateAsync();
                }
                catch (Exception)
                {
                    var i = 0;
                    i++;
                }
            },
            new EventLogSequenceNumberTokenWithFilter(State.Offset, eventTypes));
    }

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        _logger.Unsubscribing(_microserviceId, _eventSequenceId, _tenantId);
        if (_subscription is not null)
        {
            await _subscription.UnsubscribeAsync();
            _subscription = null;
        }
    }

    /// <inheritdoc/>
    public void Disconnected()
    {
        Unsubscribe().Wait();
    }

    bool HasDefinitionChanged(IEnumerable<EventType> eventTypes) => !State.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(eventTypes.OrderBy(_ => _.Id.Value));
}
