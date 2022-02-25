// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Connections;
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
    readonly ConcurrentDictionary<Guid, StreamSubscriptionHandle<AppendedEvent>> _subscriptions = new();
    readonly IExecutionContextManager _executionContextManager;
    readonly IRequestContextManager _requestContextManager;
    readonly IConnectedClients _connectedObservers;
    readonly ILogger<Observer> _logger;
    IAsyncStream<AppendedEvent>? _stream;
    ObserverId _observerId = Guid.Empty;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observer"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with the Orleans request context.</param>
    /// <param name="connectedClients"><see cref="IConnectedClients"/>.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public Observer(
        IExecutionContextManager executionContextManager,
        IRequestContextManager requestContextManager,
        IConnectedClients connectedClients,
        ILogger<Observer> logger)
    {
        _executionContextManager = executionContextManager;
        _requestContextManager = requestContextManager;
        _connectedObservers = connectedClients;
        _logger = logger;
        _connectedObservers.ClientDisconnected += async (_) =>
        {
            foreach (var (subscriptionId, handler) in _subscriptions)
            {
                await Unsubscribe(subscriptionId);
            }
        };
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var eventSequenceIdAsString);
        _eventSequenceId = eventSequenceIdAsString;
        _microserviceId = _executionContextManager.Current.MicroserviceId;
        _tenantId = _executionContextManager.Current.TenantId;

        var streamProvider = GetStreamProvider(EventSequence.StreamProvider);
        _stream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, _tenantId.ToString());

        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task Subscribe(IEnumerable<EventType> eventTypes)
    {
        var connectionId = _requestContextManager.Get(RequestContextKeys.ConnectionId).ToString()!;
        var subscriptionHandle = await _stream!.SubscribeAsync(
            async (@event, _) =>
            {
                if (!_connectedObservers.AnyConnectedClients && connectionId != ConnectionId.Kernel)
                {
                    return;
                }

                var key = new PartitionedObserverKey(_microserviceId, _tenantId, _eventSequenceId, @event.Context.EventSourceId);
                var partitionedObserver = GrainFactory.GetGrain<IPartitionedObserver>(_observerId, keyExtension: key.ToString());
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

        _subscriptions[subscriptionHandle.HandleId] = subscriptionHandle;
    }

    /// <inheritdoc/>
    public async Task Unsubscribe(Guid subscriptionId)
    {
        if (_subscriptions.ContainsKey(subscriptionId))
        {
            try
            {
                await _subscriptions[subscriptionId].UnsubscribeAsync();
            }
            catch (Exception)
            {
                _logger.UnsubscribeFailedSubscriptionUnavailable(subscriptionId);
            }
            _subscriptions.Remove(subscriptionId, out _);
        }
    }
}
