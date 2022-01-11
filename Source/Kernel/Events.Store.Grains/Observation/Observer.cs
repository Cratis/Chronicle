// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Events.Store.Observation;
using Cratis.Execution;
using Cratis.Extensions.Orleans.Execution;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IObserver"/>.
    /// </summary>
    [StorageProvider(ProviderName = ObserverState.StorageProvider)]
    public class Observer : Grain<ObserverState>, IObserver
    {
        readonly ConcurrentDictionary<Guid, StreamSubscriptionHandle<AppendedEvent>> _subscriptions = new();
        readonly IRequestContextManager _requestContextManager;
        readonly IConnectedClients _connectedObservers;
        IAsyncStream<AppendedEvent>? _stream;
        ObserverId _observerId = Guid.Empty;
        TenantId _tenantId = TenantId.NotSet;
        EventLogId _eventLogId = EventLogId.Unspecified;

        /// <summary>
        /// Initializes a new instance of the <see cref="Observer"/> class.
        /// </summary>
        /// <param name="requestContextManager"></param>
        /// <param name="connectedClients"></param>
        public Observer(
            IRequestContextManager requestContextManager,
            IConnectedClients connectedClients)
        {
            _requestContextManager = requestContextManager;
            _connectedObservers = connectedClients;
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
            _observerId = this.GetPrimaryKey(out var eventLogIdAsString);
            var tenantIdAsString = _requestContextManager.Get(RequestContextKeys.TenantId)!.ToString();
            _tenantId = tenantIdAsString!;
            _eventLogId = eventLogIdAsString;

            var streamProvider = GetStreamProvider(EventLog.StreamProvider);
            _stream = streamProvider.GetStream<AppendedEvent>(_eventLogId, _tenantId.ToString());

            await base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task Subscribe(IEnumerable<EventType> eventTypes)
        {
            var connectionId = _requestContextManager.Get(RequestContextKeys.ConnectionId).ToString()!;
            var subscriptionHandle = await _stream!.SubscribeAsync(
                async (@event, _) =>
                {
                    if (!_connectedObservers.AnyConnectedClients)
                    {
                        return;
                    }

                    var key = PartitionedObserverKeyHelper.Create(_tenantId, _eventLogId, @event.EventContext.EventSourceId);
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
                }, new EventLogSequenceNumberTokenWithFilter(State.Offset, eventTypes));

            _subscriptions[subscriptionHandle.HandleId] = subscriptionHandle;
        }

        /// <inheritdoc/>
        public async Task Unsubscribe(Guid subscriptionId)
        {
            if (_subscriptions.ContainsKey(subscriptionId))
            {
                await _subscriptions[subscriptionId].UnsubscribeAsync();
                _subscriptions.Remove(subscriptionId, out _);
            }
        }
    }
}
